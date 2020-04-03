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
  public class NugetDependencyFileGenerator
  {
    private readonly string _nugetConfig;
    private readonly IPackageSourceResolver _packageSourceResolver;

    public NugetDependencyFileGenerator(string nugetConfig, IPackageSourceResolver packageSourceResolver = null)
    {
      _nugetConfig = nugetConfig;
      _packageSourceResolver = packageSourceResolver;
    }

    private async Task<(WorkspaceEntryBuilder workspaceEntryBuilder, LocalPackageWithGroups[])> ResolveLocalPackages(string targetFramework, string targetRuntime,
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

        var workspaceEntryBuilder = new WorkspaceEntryBuilder(dependencyGraph.Conventions, _packageSourceResolver)
          .WithTarget(new FrameworkRuntimePair(NuGetFramework.Parse(targetFramework), targetRuntime));

        // TODO split workspace entry builder logic
        return (workspaceEntryBuilder, localPackages.Select(workspaceEntryBuilder.ResolveGroups).ToArray());
      }
    }

    private async Task<IReadOnlyCollection<WorkspaceEntry>> CreateEntries(string targetFramework, string targetRuntime,
      IEnumerable<(string package, string version)> packageReferences)
    {
      // First resolve al file groups
      var (workspaceEntryBuilder, resolved) = await ResolveLocalPackages(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);

      // Then we use them to validate deps actually contain content
      workspaceEntryBuilder.WithLocalPackages(resolved);

      return resolved.SelectMany(workspaceEntryBuilder.Build)
        .Where(entry => !SdkList.Dlls.Contains(entry.PackageIdentity.Id.ToLower()))
        .ToArray();
    }

    public async Task<string> GenerateDeps(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      var entries = await CreateEntries(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);
      return string.Join(string.Empty, entries.Select(entry => entry.Generate(indent: true)));
    }

    public async Task WriteRepository(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      var (_, packages) = await ResolveLocalPackages(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);

      var symlinks = new HashSet<(string, string)>();

      foreach(var entryGroup in packages.GroupBy(e => e.LocalPackageSourceInfo.Package.Id, StringComparer.OrdinalIgnoreCase))
      {
        var id = entryGroup.Key.ToLower();

        var content = $@"package(default_visibility = [""//visibility:public""])
load(""@io_bazel_rules_dotnet//dotnet:defs.bzl"", ""core_import_library"")

{string.Join("\n\n", entryGroup.Select(CreateTarget))}";

        var filePath = $"{id}/BUILD";
        new FileInfo(filePath).Directory.Create();
        File.WriteAllText(filePath, content);

        // Possibly link multiple versions
        foreach(var entry in entryGroup)
        {
          symlinks.Add(($"{id}/{entry.LocalPackageSourceInfo.Package.Version}", entry.LocalPackageSourceInfo.Package.ExpandedPath));
        }
      }

      File.WriteAllText("link.cmd", string.Join("\n", symlinks.Select(sl => $@"mklink /D ""{sl.Item1}"" ""{sl.Item2}""")));
      Process.Start(new ProcessStartInfo("cmd.exe", "/C link.cmd")).WaitForExit();
    }

    private string CreateTarget(LocalPackageWithGroups package)
    {
      var identity = package.LocalPackageSourceInfo.Package;
      var libs = Array(package.RuntimeItemGroups.SingleOrDefault()?.Items.Select(v => $"{identity.Version}/{v}"));
      var refs = Array(package.RefItemGroups.SingleOrDefault()?.Items.Select(v => $"{identity.Version}/{v}"));
      var deps = Array(package.DependencyGroups.SingleOrDefault()?.Packages
        .Where(p => !SdkList.Dlls.Contains(p.Id.ToLower()))
        .Select(p => $"//{p.Id.ToLower()}:netcoreapp3.1_core"));

      return $@"core_import_library(
  name = ""netcoreapp3.1_core"",
  libs = [{libs}],
  refs = [{refs}],
  deps = [{deps}],
  version = ""{identity.Version}"",
)

filegroup(
  name = ""files"",
  srcs = glob([""{identity.Version}/**""]),
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