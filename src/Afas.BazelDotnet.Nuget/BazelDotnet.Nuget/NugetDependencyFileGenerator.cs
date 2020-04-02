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

    private async Task<IReadOnlyCollection<WorkspaceEntry>> CreateEntries(string targetFramework, string targetRuntime,
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
        var localPackages = await dependencyGraphResolver.ResolveLocalPackages(dependencyGraph).ConfigureAwait(false);

        var workspaceEntryBuilder = new WorkspaceEntryBuilder(dependencyGraph.Conventions, _packageSourceResolver)
          .WithTarget(new FrameworkRuntimePair(NuGetFramework.Parse(targetFramework), targetRuntime));

        // First resolve al file groups
        var resolved = localPackages.Select(workspaceEntryBuilder.ResolveGroups).ToArray();

        // Then we use them to validate deps actually contain content
        workspaceEntryBuilder.WithLocalPackages(resolved);

        return resolved.SelectMany(workspaceEntryBuilder.Build)
          .Where(entry => !SdkList.Dlls.Contains(entry.PackageIdentity.Id.ToLower()))
          .ToArray();
      }
    }

    public async Task<string> GenerateDeps(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      var entries = await CreateEntries(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);
      return string.Join(string.Empty, entries.Select(entry => entry.Generate(indent: true)));
    }

    public async Task WriteRepository(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      var entries = await CreateEntries(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);

      var symlinks = new HashSet<(string, string)>();

      foreach(var entryGroup in entries.GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
      {
        var id = entryGroup.Key.ToLower();

        var content = string.Join("\n", entryGroup
          .Where(e => e.CoreLib.ContainsKey("netcoreapp3.1"))
          .Select(e => $@"
core_import_library(
  name = ""netcoreapp3.1_core"",
  src = ""{e.PackageIdentity.Version}/{e.CoreLib["netcoreapp3.1"]}"",
  deps = [
    {string.Join(",\n    ", e.Core_Deps.ContainsKey("netcoreapp3.1") ? e.Core_Deps["netcoreapp3.1"].Select(Fix) : Array.Empty<string>())}
  ],
  version = ""{e.PackageIdentity.Version}"",
)
filegroup(
  name = ""files"",
  srcs = glob([""{e.PackageIdentity.Version}/**""]),
  visibility = [""//visibility:public""]
)
"));
        var prefix = @"
package(default_visibility = [ ""//visibility:public"" ])
load(""@io_bazel_rules_dotnet//dotnet:defs.bzl"", ""core_import_library"")
";

        var filePath = $"{id}/BUILD";
        (new FileInfo(filePath)).Directory.Create();
        File.WriteAllText(filePath, prefix + content);

        // Possibly link multiple versions
        foreach(var entry in entryGroup)
        {
          symlinks.Add(($"{id}/{entry.PackageIdentity.Version}", entry.ExpandedPath));
        }
      }

      File.WriteAllText("link.cmd", string.Join("\n", symlinks.Select(sl => $@"mklink /D ""{sl.Item1}"" ""{sl.Item2}""")));
      Process.Start(new ProcessStartInfo("cmd.exe", "/C link.cmd")).WaitForExit();

      string Fix(string s) => $@"""{s.Replace("//", "").Replace("@", "@nuget//")}""";
    }
  }
}