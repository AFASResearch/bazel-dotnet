using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.RuntimeModel;
using NuGet.Versioning;

namespace Afas.BazelDotnet.Nuget
{
  public class NugetRepositoryGenerator
  {
    private class TargetFrameworkProcessor
    {
      public string CreateConfigurationTargets(IReadOnlyList<string> targetFrameworks) => $@"
load(""@bazel_skylib//rules:common_settings.bzl"", ""string_flag"")

string_flag(
    name = ""framework"",
    values = {Indent(StringArray(targetFrameworks))},
    build_setting_default = ""{targetFrameworks.First()}""
)

{string.Join("\n", targetFrameworks
  .Select(f => $@"config_setting(
  name = ""frameworks-{f}"",
  flag_values = {{
      "":framework"": ""{f}""
  }}
)
"))}";

      public string CreateSelectStatementWhenApplicable(IReadOnlyList<FrameworkSpecificGroup> groups, Func<string, string> format)
      {
        var arrays = groups.Select(g => StringArray(g.Items.Select(format))).ToArray();
        return arrays.Distinct().Count() switch
        {
          0 => "[]",
          1 => arrays[0],
          _ => Select(Indent(Dict(groups.Select(g => $"//:frameworks-{g.TargetFramework.GetShortFolderName()}")
            .Zip(arrays)
            .Skip(1).Append(("//conditions:default", arrays[0]))
            .ToDictionary(t => t.Item1, t => t.Item2))))
        };
      }

      private static string Select(string input) => $@"select({input})";
    }

    private readonly string _nugetConfig;
    private readonly ILookup<string, (string target, string configSetting)> _imports;
    private readonly TargetFrameworkProcessor _targetFrameworkProcessor = new();

    public NugetRepositoryGenerator(string nugetConfig, ILookup<string, (string target, string configSetting)> imports)
    {
      _nugetConfig = nugetConfig;
      _imports = imports;
    }

    private async Task<PackagesLockFile> TryReadLockFile(string nugetLockFilePath, ILogger logger)
    {
      if(string.IsNullOrEmpty(nugetLockFilePath))
      {
        return null;
      }

      if(!File.Exists(nugetLockFilePath))
      {
        throw new Exception("Nuget Lock File was specified but not found.");
      }

      return PackagesLockFileFormat.Parse(await File.ReadAllTextAsync(nugetLockFilePath), logger, nugetLockFilePath);
    }

    private async Task<NugetRepositoryEntry[]> ResolveLocalPackages(IReadOnlyList<string> targetFrameworks, string targetRuntime,
      IEnumerable<(string package, string version)> packageReferences, string nugetLockFilePath)
    {
      ILogger logger = new ConsoleLogger();

      var rootProject = new RootProject(
        targetFrameworks.Select(tfm => new FrameworkRuntimePair(NuGetFramework.Parse(tfm), targetRuntime)).ToArray(),
        packageReferences.Select(r => new LibraryRange(r.package, VersionRange.Parse(r.version), LibraryDependencyTarget.Package)).ToArray(),
        await TryReadLockFile(nugetLockFilePath, logger));

      var settings = Settings.LoadSpecificSettings(Path.GetDirectoryName(_nugetConfig), Path.GetFileName(_nugetConfig));
      DefaultCredentialServiceUtility.SetupDefaultCredentialService(logger, nonInteractive: true);

      // ~/.nuget/packages
      using var cache = new SourceCacheContext();

      var dependencyGraphResolver = new TransitiveDependencyResolver(settings, logger, cache, rootProject);

      var dependencyGraphs = await dependencyGraphResolver.ResolveGraphs();

      if(!string.IsNullOrEmpty(nugetLockFilePath) && !rootProject.IsLockFileValid)
      {
        await File.WriteAllTextAsync(nugetLockFilePath, dependencyGraphResolver.RenderLockFile(dependencyGraphs));
      }

      var runtimeGraph = dependencyGraphs.Select(g => g.RuntimeGraph).Aggregate(RuntimeGraph.Merge);
      var entryBuilder = new NugetRepositoryEntryBuilder(new ManagedCodeConventions(runtimeGraph), rootProject.Targets);

      var localPackages = await dependencyGraphResolver.DownloadPackages(dependencyGraphs.Take(2)).ConfigureAwait(false);
      var entries = localPackages.Select(entryBuilder.ResolveGroups).ToArray();

      var (frameworkEntries, frameworkOverrides) = await new FrameworkDependencyResolver(dependencyGraphResolver)
          // TODO ResolveFrameworkPackages on multiple tfms
          .ResolveFrameworkPackages(entries, targetFrameworks.First())
        .ConfigureAwait(false);

      var overridenEntries = entries.Select(p =>
        frameworkOverrides.TryGetValue(p.LocalPackageSourceInfo.Package.Id, out var frameworkOverride)
          ? entryBuilder.BuildFrameworkOverride(p, frameworkOverride)
          : p);

      return frameworkEntries.Concat(overridenEntries).ToArray();
    }

    public async Task WriteRepository(IReadOnlyList<string> targetFrameworks, string targetRuntime, IEnumerable<(string package, string version)> packageReferences, string nugetLockFilePath)
    {
      var packages = await ResolveLocalPackages(targetFrameworks, targetRuntime, packageReferences, nugetLockFilePath).ConfigureAwait(false);

      var symlinks = new HashSet<(string link, string target)>();

      foreach(var entryGroup in packages.GroupBy(e => e.LocalPackageSourceInfo.Package.Id, StringComparer.OrdinalIgnoreCase))
      {
        var id = entryGroup.Key.ToLower();
        bool isSingle = entryGroup.Count() == 1;

        if(isSingle)
        {
          WriteBuildFile(entryGroup.Single(), id);
        }
        else
        {
          WriteBuildFile(entryGroup, id);
        }

        // Possibly link multiple versions
        foreach(var entry in entryGroup)
        {
          symlinks.Add(($"{id}/{entry.LocalPackageSourceInfo.Package.Version}", entry.LocalPackageSourceInfo.Package.ExpandedPath));

          if(isSingle)
          {
            symlinks.Add(($"{id}/current", entry.LocalPackageSourceInfo.Package.ExpandedPath));
          }
        }
      }

      File.WriteAllText("BUILD", _targetFrameworkProcessor.CreateConfigurationTargets(targetFrameworks));

      File.WriteAllText("symlinks_manifest", string.Join("\n", symlinks
        .Select(sl => $@"{sl.link} {sl.target}")));

      File.WriteAllText("link.cmd", @"
for /F ""usebackq tokens=1,2 delims= "" %%i in (""symlinks_manifest"") do mklink /J ""%%i"" ""%%j""
exit /b %errorlevel%
");
      var proc = Process.Start(new ProcessStartInfo("cmd.exe", "/C link.cmd"));
      proc.WaitForExit();

      if(proc.ExitCode != 0)
      {
        throw new Exception("Creating symlinks exited non 0");
      }
    }

    private static readonly HashSet<string> _exportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
      ".exe",
      ".ruleset",
      ".json",
      // only really used for internal redirects. Can we do this differently?
      ".dll",
    };

    private void WriteBuildFile(NugetRepositoryEntry entry, string id)
    {
      var exports = StringArray(entry.LocalPackageSourceInfo.Package.Files.Where(f => _exportedExtensions.Contains(Path.GetExtension(f)))
        .Select(f => $"current/{f}")
        .Prepend("contentfiles.txt"));

      var content = $@"package(default_visibility = [""//visibility:public""])
load(""@io_bazel_rules_dotnet//dotnet:defs.bzl"", ""core_import_library"")

exports_files({exports})

{CreateTarget(entry)}";

      var filePath = $"{id}/BUILD";
      new FileInfo(filePath).Directory.Create();
      File.WriteAllText(filePath, content);

      // Also write a special file that lists the content files in this package.
      // This is to work around the fact that we cannot easily expose folders.
      File.WriteAllLines($"{id}/contentfiles.txt", GetContentFiles(entry).Select(v => $"current/{v}"));
    }

    private void WriteBuildFile(IGrouping<string, NugetRepositoryEntry> entryGroup, string id)
    {
      var content = $@"package(default_visibility = [""//visibility:public""])
load(""@io_bazel_rules_dotnet//dotnet:defs.bzl"", ""core_import_library"")

{string.Join("\n\n", entryGroup.Select(e => CreateTarget(e)))}";

      var filePath = $"{id}/BUILD";
      new FileInfo(filePath).Directory.Create();
      File.WriteAllText(filePath, content);
    }

    private IEnumerable<string> GetContentFiles(NugetRepositoryEntry package)
    {
      var group = package.ContentFileGroups.FirstOrDefault();
      if(group?.Items.Any() == true)
      {
        // Symlink optimization to link the entire group folder e.g. contentFiles/any/netcoreapp3.1
        // We assume all files in a group have the same prefix
        //yield return string.Join('/', group.Items.First().Split('/').Take(3));

        foreach(var item in group.Items)
        {
          yield return item;
        }
      }
    }

    private string CreateTarget(NugetRepositoryEntry package)
    {
      var identity = package.LocalPackageSourceInfo.Package;
      var folder = identity.Version.ToString();

      IEnumerable<string> Elems()
      {
        yield return $@"filegroup(
  name = ""content_files"",
  srcs = {StringArray(GetContentFiles(package).Select(v => $"{folder}/{v}"))},
)";

        var name = identity.Id.ToLower();

        if(_imports.Contains(name))
        {
          var selects = _imports[name].ToDictionary(i => i.configSetting, i => Quote(i.target));
          name += "__nuget";
          selects["//conditions:default"] = Quote(name);

          yield return $@"alias(
  name = ""{identity.Id.ToLower()}"",
  actual = select({Indent(Dict(selects))})
)";
        }

        bool hasDebugDlls = package.DebugRuntimeItemGroups.Count != 0;
        var libs = _targetFrameworkProcessor.CreateSelectStatementWhenApplicable(package.RuntimeItemGroups, v => $"{folder}/{v}");

        if(hasDebugDlls)
        {
          yield return @"
config_setting(
  name = ""compilation_mode_dbg"",
  values = {
    ""compilation_mode"": ""dbg"",
  },
)";
          libs = Indent($@"select({{
  "":compilation_mode_dbg"": {StringArray(package.DebugRuntimeItemGroups.First().Items.Select(v => $"{folder}/{v}"))},
  ""//conditions:default"": {libs},
}})");
        }

        var refs = _targetFrameworkProcessor.CreateSelectStatementWhenApplicable(package.RefItemGroups, v => v.StartsWith("//") ? v : $"{folder}/{v}");

        yield return $@"core_import_library(
  name = ""{name}"",
  libs = {libs},
  refs = {refs},
  analyzers = {StringArray(package.AnalyzerItemGroups.FirstOrDefault()?.Items.Select(v => v.StartsWith("//") ? v : $"{folder}/{v}"))},
  deps = {StringArray(package.DependencyGroups.FirstOrDefault()?.Packages.Where(ShouldIncludeDep).Select(p => $"//{p.Id.ToLower()}"))},
  data = ["":content_files""],
  version = ""{identity.Version}"",
)";
      }

      return string.Join("\n\n", Elems());
    }

    private bool ShouldIncludeDep(PackageDependency package) =>
      // Some libraries depend on netstandard. It will not include anything of use
      // Should only be referenced explicitly as targetframework
      !string.Equals(package.Id, "netstandard.library", StringComparison.OrdinalIgnoreCase);

    private static string Indent(string input)
    {
      var lines = input.Split('\n');
      if(lines.Length > 1)
      {
        return $"{lines[0]}\n{string.Join('\n', lines[1..].Select(l => "" + $"  {l}"))}";
      }
      return lines[0];
    }

    private static string StringArray(IEnumerable<string> items) => items?.Any() != true ? "[]" : Indent($@"[
{string.Join(",\n", items.Select(i => $@"  ""{i}"""))}
]");

    private static string Quote(string input) => $@"""{input}""";

    private static string Dict(IReadOnlyDictionary<string, string> items)
    {
      var s = new StringBuilder();

      s.Append("{\n");

      foreach(var (key, value) in items)
      {
        s.Append($@"  ""{key}"": {value},
");
      }

      s.Append("}");

      return s.ToString();
    }
  }
}