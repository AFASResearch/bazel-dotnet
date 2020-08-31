using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Protocol.Core.Types;

namespace Afas.BazelDotnet.Nuget
{
  public class NugetRepositoryGenerator
  {
    private readonly string _nugetConfig;

    public NugetRepositoryGenerator(string nugetConfig)
    {
      _nugetConfig = nugetConfig;
    }

    private async Task<NugetRepositoryEntry[]> ResolveLocalPackages(string targetFramework, string targetRuntime,
      IEnumerable<(string package, string version)> packageReferences)
    {
      ILogger logger = new ConsoleLogger();
      var settings = Settings.LoadSpecificSettings(Path.GetDirectoryName(_nugetConfig), Path.GetFileName(_nugetConfig));

      // ~/.nuget/packages

      using(var cache = new SourceCacheContext())
      {
        var dependencyGraphResolver = new TransitiveDependencyResolver(settings, logger, cache);

        foreach((string package, string version) v in packageReferences)
        {
          dependencyGraphResolver.AddPackageReference(v.package, v.version);
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
    }

    public async Task WriteRepository(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      var packages = await ResolveLocalPackages(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);

      var symlinks = new HashSet<(string, string)>();

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

      File.WriteAllText("link.cmd", string.Join("\n", symlinks
        .Select(sl => $@"mklink /J ""{sl.Item1}"" ""{sl.Item2}""")
        .Append("exit /b %errorlevel%")));
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

{CreateTarget(entry, isSingle: true)}";

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

{string.Join("\n\n", entryGroup.Select(e => CreateTarget(e, isSingle: false)))}";

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

    private string CreateTarget(NugetRepositoryEntry package, bool isSingle)
    {
      var identity = package.LocalPackageSourceInfo.Package;
      var folder = isSingle ? "current" : identity.Version.ToString();
      var libs = Array(package.RuntimeItemGroups.SingleOrDefault()?.Items.Select(v => $"{folder}/{v}"));
      var refs = Array(package.RefItemGroups.SingleOrDefault()?.Items.Select(v => v.StartsWith("//") ? v : $"{folder}/{v}"));
      var contentFiles = Array(GetContentFiles(package).Select(v => $"{folder}/{v}"));
      var analyzers = Array(package.AnalyzerItemGroups.SingleOrDefault()?.Items.Select(v => v.StartsWith("//") ? v : $"{folder}/{v}"));

      var deps = Array(package.DependencyGroups.SingleOrDefault()?.Packages
        //.Where(p => !SdkList.Dlls.Contains(p.Id.ToLower()))
        .Select(p => $"//{p.Id.ToLower()}:netcoreapp3.1_core"));

      return $@"exports_files(glob([""{folder}/**"", ""{identity.Version}/**""]))

filegroup(
    name = ""content_files"",
    srcs = [{contentFiles}],
)

core_import_library(
  name = ""netcoreapp3.1_core"",
  libs = [{libs}],
  refs = [{refs}],
  analyzers = [{analyzers}],
  deps = [{deps}],
  data = ["":content_files""],
  version = ""{identity.Version}"",
)";
    }

    private string Array(IEnumerable<string> elems)
    {
      if(elems?.Any() != true)
      {
        return string.Empty;
      }

      return $"\n{string.Join(",\n", elems.Select(e => $"    \"{e}\""))}\n  ";
    }
  }
}