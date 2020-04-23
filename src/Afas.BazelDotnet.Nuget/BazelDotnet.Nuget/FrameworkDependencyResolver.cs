using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Repositories;
using NuGet.Versioning;

namespace Afas.BazelDotnet.Nuget
{
  internal class FrameworkDependencyResolver
  {
    private readonly IReadOnlyDictionary<string, string> _frameworkReferenceTargetPackMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Microsoft.NETCore.App"] = "Microsoft.NETCore.App.Ref",
      ["Microsoft.AspNetCore.App"] = "Microsoft.AspNetCore.App.Ref",
    };

    private readonly IReadOnlyDictionary<string, string> _frameworkReferenceVersionsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["netcoreapp3.0"] = "3.0.0",
      ["netcoreapp3.1"] = "3.1.0",
    };

    private readonly TransitiveDependencyResolver _dependencyResolver;

    internal FrameworkDependencyResolver(TransitiveDependencyResolver dependencyResolver)
    {
      _dependencyResolver = dependencyResolver;
    }

    public async Task<(IReadOnlyCollection<NugetRepositoryEntry> entries, IReadOnlyDictionary<string, string> overrides)> ResolveFrameworkPackages(
      IReadOnlyCollection<NugetRepositoryEntry> localPackages, string targetFramework)
    {
      var packagesLookup = localPackages.ToDictionary(l => l.LocalPackageSourceInfo.Package.Id, l => l, StringComparer.OrdinalIgnoreCase);
      var entries = new List<NugetRepositoryEntry>();
      var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      var localTargetPackages = await Task.WhenAll(
        GetTargetPackages(localPackages)
          .Select(p => DownloadTargetPackage(p, targetFramework))
      ).ConfigureAwait(false);

      foreach(var targetPackage in localTargetPackages)
      {
        var frameworkList = GetFrameworkList(targetPackage, targetFramework);
        var packageOverrides = GetPackageOverrides(targetPackage);

        var potentialOverrides = packageOverrides.Keys.
          Intersect(packagesLookup.Keys, StringComparer.OrdinalIgnoreCase);
        var frameworkConflicts = frameworkList.Keys
          .Intersect(packagesLookup.Keys, StringComparer.OrdinalIgnoreCase)
          .Except(packageOverrides.Keys, StringComparer.OrdinalIgnoreCase);

        var considerAssemblyVersions = new List<string>();
        // Keep track of a list of packages that have higher versions than the targetPackage provided ones
        var winningPackages = new Dictionary<string, LocalPackageInfo>();

        foreach(var o in potentialOverrides)
        {
          if(!frameworkList.ContainsKey(o))
          {
            continue;
          }

          var targetIsEmpty = packagesLookup[o].RefItemGroups.SingleOrDefault()?.Items.Any() != true;
          if(targetIsEmpty || packageOverrides[o] >= packagesLookup[o].Version)
          {
            overrides.Add(o, $"//{targetPackage.Package.Id.ToLower()}:{targetPackage.Package.Version}/{frameworkList[o].file}");
          }
          else
          {
            considerAssemblyVersions.Add(o);
          }
        }

        foreach(var c in frameworkConflicts.Concat(considerAssemblyVersions))
        {
          if(OverridesAssemblyVersion(c, frameworkList[c].version))
          {
            overrides.Add(c, $"//{targetPackage.Package.Id.ToLower()}:{targetPackage.Package.Version}/{frameworkList[c].file}");
          }
          else
          {
            winningPackages.Add(c, packagesLookup[c].LocalPackageSourceInfo.Package);
          }
        }

        entries.Add(
          BuildEntry(targetPackage, targetFramework, frameworkList, winningPackages)
        );
      }

      return (entries, overrides);

      bool OverridesAssemblyVersion(string packageId, Version frameworkListVersion)
      {
        var refItems = packagesLookup[packageId].RefItemGroups.Single();
        if(!refItems.Items.Any())
        {
          return true;
        }
        var refAssembly = refItems.Items.Single();

        var conflictingFile = Path.Combine($"{packagesLookup[packageId].LocalPackageSourceInfo.Package.ExpandedPath}/{refAssembly}");
        var conflictingVersion = FileUtilities.GetAssemblyVersion(conflictingFile);

        return frameworkListVersion >= conflictingVersion;
      }
    }

    private IEnumerable<string> GetTargetPackages(IReadOnlyCollection<NugetRepositoryEntry> localPackages)
    {
      // see core_sdk\core\sdk\3.1.100\Microsoft.NETCoreSdk.BundledVersions.props
      var frameworkRefs = localPackages
        // TODO consider target framework when multiple specified
        .SelectMany(l => l.LocalPackageSourceInfo.Package.Nuspec
          .GetFrameworkRefGroups()
          .SelectMany(g => g.FrameworkReferences))
        // Temp solution: this is not included in packages props and should be included as an additional argument instead
        .Prepend(new FrameworkReference("Microsoft.AspNetCore.App"))
        .Distinct()
        // Implicit dependency of al netcoreapp projects
        .Prepend(new FrameworkReference("Microsoft.NETCore.App"))
        .ToArray();
      return frameworkRefs.Select(r => _frameworkReferenceTargetPackMap[r.Name]);
    }

    private NugetRepositoryEntry BuildEntry(
      LocalPackageSourceInfo targetPackage,
      string targetFramework,
      Dictionary<string, (string file, Version version)> frameworkList,
      Dictionary<string, LocalPackageInfo> winningPackages)
    {
      var nuGetFramework = NuGetFramework.Parse(targetFramework);

      return new NugetRepositoryEntry(
        targetPackage,
        refItemGroups: new []
        {
          new FrameworkSpecificGroup(
            nuGetFramework,
            frameworkList.Where(pair => !winningPackages.ContainsKey(pair.Key)).Select(pair => pair.Value.file).ToArray())
        },
        runtimeItemGroups: Array.Empty<FrameworkSpecificGroup>(),
        dependencyGroups: new []
        {
          new PackageDependencyGroup(
            nuGetFramework,
            winningPackages.Select(pair => new PackageDependency(pair.Key)).ToArray()), 
        });
    }

    private async Task<LocalPackageSourceInfo> DownloadTargetPackage(string targetPackId, string targetFramework)
    {
      var packGraph = await _dependencyResolver
        .ResolveFrameworkReference(targetPackId, _frameworkReferenceVersionsMap[targetFramework], targetFramework).ConfigureAwait(false);

      var packNode = packGraph.Flattened.Single();
      return await _dependencyResolver.DownloadPackage(packNode).ConfigureAwait(false);
    }

    private Dictionary<string, (string file, Version version)> GetFrameworkList(LocalPackageSourceInfo targetPackage, string targetFramework)
    {
      var frameworkListPath = "data/FrameworkList.xml";

      if(!targetPackage.Package.Files.Contains(frameworkListPath))
      {
        throw new Exception($"No data/FrameworkList.xml found in {targetPackage.Package.Id}");
      }

      return XDocument.Load(Path.Combine(targetPackage.Package.ExpandedPath, frameworkListPath))
        .Descendants("File")
        .ToDictionary(
          f => f.Attribute("AssemblyName").Value,
          f => (file: FixPath(f.Attribute("Path").Value), version: Version.Parse(f.Attribute("AssemblyVersion").Value)),
          StringComparer.OrdinalIgnoreCase);

      // Some inconsistency in FrameworkList
      string FixPath(string v) => v.StartsWith("ref/") ? v : $"ref/{targetFramework}/{v}";
    }

    private IReadOnlyDictionary<string, NuGetVersion> GetPackageOverrides(LocalPackageSourceInfo targetPackage)
    {
      var packageOverridesPath = "data/PackageOverrides.txt";

      if(!targetPackage.Package.Files.Contains(packageOverridesPath))
      {
        return new Dictionary<string, NuGetVersion>();
      }

      return File.ReadAllLines(Path.Combine(targetPackage.Package.ExpandedPath, packageOverridesPath))
        .Select(l => l.Split("|"))
        .ToDictionary(s => s[0], s => NuGetVersion.Parse(s[1]), StringComparer.OrdinalIgnoreCase);
    }
  }
}
