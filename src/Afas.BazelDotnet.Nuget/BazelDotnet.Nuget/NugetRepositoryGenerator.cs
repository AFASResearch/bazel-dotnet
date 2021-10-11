using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;

namespace Afas.BazelDotnet.Nuget
{
  public class NugetRepositoryGenerator
  {
    private readonly string _nugetConfig;
    private readonly ILookup<string, (string target, string configSetting)> _imports;

    public NugetRepositoryGenerator(string nugetConfig, ILookup<string, (string target, string configSetting)> imports)
    {
      _nugetConfig = nugetConfig;
      _imports = imports;
    }

    private async Task<NugetRepositoryEntry[]> ResolveLocalPackages(string targetFramework, string targetRuntime,
      IEnumerable<(string package, string version)> packageReferences)
    {
      ILogger logger = new ConsoleLogger();
      var settings = Settings.LoadSpecificSettings(Path.GetDirectoryName(_nugetConfig), Path.GetFileName(_nugetConfig));
      DefaultCredentialServiceUtility.SetupDefaultCredentialService(logger, nonInteractive: true);

      // ~/.nuget/packages
      using var cache = new SourceCacheContext();

      var dependencyGraphResolver = new TransitiveDependencyResolver(settings, logger, cache);

      foreach(var (package, version) in packageReferences)
      {
        dependencyGraphResolver.AddPackageReference(package, version);
      }

      var dependencyGraph = await dependencyGraphResolver.ResolveGraph(targetFramework, targetRuntime).ConfigureAwait(false);
      var localPackages = await dependencyGraphResolver.DownloadPackages(dependencyGraph).ConfigureAwait(false);

      var entryBuilder = new NugetRepositoryEntryBuilder(dependencyGraph.Conventions)
          .WithTarget(new FrameworkRuntimePair(NuGetFramework.Parse(targetFramework), targetRuntime));

      var entries = localPackages.Select(entryBuilder.ResolveGroups).ToArray();

        var (frameworkEntries, frameworkOverrides) = await new FrameworkDependencyResolver(dependencyGraphResolver)
          .ResolveFrameworkPackages(entries, targetFramework)
        .ConfigureAwait(false);

      var overridenEntries = entries.Select(p =>
        frameworkOverrides.TryGetValue(p.LocalPackageSourceInfo.Package.Id, out var frameworkOverride)
          ? entryBuilder.BuildFrameworkOverride(p, frameworkOverride)
          : p);

      return frameworkEntries.Concat(overridenEntries).ToArray();
    }

    public async Task WriteRepository(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      var packages = await ResolveLocalPackages(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);

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

    private void WriteBuildFile(NugetRepositoryEntry entry, string id)
    {
      var content = $@"package(default_visibility = [""//visibility:public""])
load(""@io_bazel_rules_dotnet//dotnet:defs.bzl"", ""core_import_library"")

exports_files([""contentfiles.txt""])

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
      var group = package.ContentFileGroups.SingleOrDefault();
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
        yield return $@"exports_files(glob([""current/**"", ""{identity.Version}/**""]))";

        yield return $@"filegroup(
  name = ""content_files"",
  srcs = {StringArray(GetContentFiles(package).Select(v => $"{folder}/{v}"))},
)";

        var name = identity.Id.ToLower();

        if(_imports.Contains(name))
        {
          var selects = _imports[name].ToDictionary(i => i.configSetting, i => i.target);
          name += "__nuget";
          selects["//conditions:default"] = name;

          yield return $@"alias(
  name = ""{identity.Id.ToLower()}"",
  actual = select({Indent(Dict(selects))})
)";
        }

        bool hasDebugDlls = package.DebugRuntimeItemGroups.Count != 0;
        var libs = StringArray(package.RuntimeItemGroups.SingleOrDefault()?.Items.Select(v => $"{folder}/{v}"));

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
  "":compilation_mode_dbg"": {StringArray(package.DebugRuntimeItemGroups.Single().Items.Select(v => $"{folder}/{v}"))},
  ""//conditions:default"": {libs},
}})");
        }


        yield return $@"core_import_library(
  name = ""{name}"",
  libs = {libs},
  refs = {StringArray(package.RefItemGroups.SingleOrDefault()?.Items.Select(v => v.StartsWith("//") ? v : $"{folder}/{v}"))},
  analyzers = {StringArray(package.AnalyzerItemGroups.SingleOrDefault()?.Items.Select(v => v.StartsWith("//") ? v : $"{folder}/{v}"))},
  deps = {StringArray(package.DependencyGroups.SingleOrDefault()?.Packages.Where(ShouldIncludeDep).Select(p => $"//{p.Id.ToLower()}"))},
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

    private static string Dict(IReadOnlyDictionary<string, string> items)
    {
      var s = new StringBuilder();

      s.Append("{\n");

      foreach(var (key, value) in items)
      {
        s.Append($@"  ""{key}"": ""{value}"",
");
      }

      s.Append("}");

      return s.ToString();
    }
  }
}