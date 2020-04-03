using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Client;
using NuGet.ContentModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Repositories;
using NuGet.Versioning;

namespace Afas.BazelDotnet.Nuget
{
  internal class WorkspaceEntryBuilder
  {
    private readonly ManagedCodeConventions _conventions;
    private readonly IPackageSourceResolver _packageSourceResolver;
    private readonly List<FrameworkRuntimePair> _targets;
    private Dictionary<string, LocalPackageWithGroups> _localPackages;

    public WorkspaceEntryBuilder(ManagedCodeConventions conventions, IPackageSourceResolver packageSourceResolver = null)
    {
      _conventions = conventions;
      _packageSourceResolver = packageSourceResolver;
      _targets = new List<FrameworkRuntimePair>();
      _localPackages = new Dictionary<string, LocalPackageWithGroups>(StringComparer.OrdinalIgnoreCase);
    }

    public WorkspaceEntryBuilder WithTarget(FrameworkRuntimePair target)
    {
      _targets.Add(target);
      return this;
    }

    public WorkspaceEntryBuilder WithTarget(NuGetFramework target)
    {
      _targets.Add(new FrameworkRuntimePair(target, runtimeIdentifier: null));
      return this;
    }

    public LocalPackageWithGroups ResolveGroups(LocalPackageSourceInfo localPackageSourceInfo)
    {
      var collection = new ContentItemCollection();
      collection.Load(localPackageSourceInfo.Package.Files);
      var allPackageDependencyGroups = localPackageSourceInfo.Package.Nuspec.GetDependencyGroups().ToArray();

      var refItemGroups = new List<FrameworkSpecificGroup>();
      var runtimeItemGroups = new List<FrameworkSpecificGroup>();
      var dependencyGroups = new List<PackageDependencyGroup>();

      foreach(var target in _targets)
      {
        SelectionCriteria criteria = _conventions.Criteria.ForFrameworkAndRuntime(target.Framework, target.RuntimeIdentifier);

        // The nunit3testadapter package publishes dll's in build/
        var buildAssemblies = new PatternSet(_conventions.Properties, new[]
        {
          new PatternDefinition("build/{tfm}/{any?}"),
          new PatternDefinition("build/{assembly?}")
        }, new[]
        {
          new PatternDefinition("build/{tfm}/{assembly}"),
          new PatternDefinition("build/{assembly}")
        });

        var bestRefGroup = collection.FindBestItemGroup(criteria,
          _conventions.Patterns.CompileRefAssemblies,
          _conventions.Patterns.CompileLibAssemblies);

        var bestRuntimeGroup = collection.FindBestItemGroup(criteria,
          _conventions.Patterns.RuntimeAssemblies,
          buildAssemblies);

        if(bestRefGroup != null)
        {
          refItemGroups.Add(new FrameworkSpecificGroup(target.Framework, bestRefGroup.Items.Select(i => i.Path)));
        }

        if(bestRuntimeGroup != null)
        {
          runtimeItemGroups.Add(new FrameworkSpecificGroup(target.Framework, bestRuntimeGroup.Items.Select(i => i.Path)));
        }

        dependencyGroups.Add(NuGetFrameworkUtility.GetNearest(allPackageDependencyGroups, target.Framework));
      }

      return new LocalPackageWithGroups(localPackageSourceInfo, refItemGroups, runtimeItemGroups, dependencyGroups);
    }

    public void WithLocalPackages(IReadOnlyCollection<LocalPackageWithGroups> localPackages)
    {
      _localPackages = localPackages.ToDictionary(p => p.LocalPackageSourceInfo.Package.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<WorkspaceEntry> Build(LocalPackageWithGroups localPackage)
    {
      var localPackageSourceInfo = localPackage.LocalPackageSourceInfo;
      var depsGroups = localPackage.DependencyGroups;

      // We do not support packages without any files
      if(!localPackage.RuntimeItemGroups.Any(g => g.Items.Any()) && !depsGroups.Any())
      {
        yield break;
      }

      // Only use deps that contain content
      depsGroups = depsGroups.Select(d => new PackageDependencyGroup(d.TargetFramework, d.Packages
        .SelectMany(p => RemoveEmptyDeps(p, d.TargetFramework)))).ToArray();

      // TODO try fix SHA
      var sha256 = "";
      string source = _packageSourceResolver?.Resolve(localPackageSourceInfo.Package.Id);
      
      var packageIdentity = new PackageIdentity(localPackageSourceInfo.Package.Id, localPackageSourceInfo.Package.Version);

      var dlls = localPackage.RuntimeItemGroups
          .SelectMany(g => g.Items)
          .Select(Path.GetFileNameWithoutExtension)
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToArray();

      // Workaround for multiple compile time dll's from a single package.
      // We add multiple packages and with the same source depending on each other
      if(dlls.Length > 1)
      {
        // Use the Package.Id dll or just the first as main
        var mainDll = dlls.FirstOrDefault(p => string.Equals(p, localPackageSourceInfo.Package.Id, StringComparison.OrdinalIgnoreCase)) ??
                      dlls.First();

        var additionalDlls = dlls.Where(d => !ReferenceEquals(mainDll, d)).ToArray();

        // Some nuget packages contain multiple dll's required at compile time.
        // The bazel rules do not support this, but we can fake this by creating mock packages for each additional dll.

        foreach(var additionalDll in additionalDlls)
        {
          yield return new WorkspaceEntry(packageIdentity, sha256,
              Array.Empty<PackageDependencyGroup>(),
              FilterSpecificDll(localPackage.RuntimeItemGroups, additionalDll),
              Array.Empty<FrameworkSpecificGroup>(),
              Array.Empty<FrameworkSpecificGroup>(), null, null,
              packageSource: source,
              name: additionalDll,
              expandedPath: localPackage.LocalPackageSourceInfo.Package.ExpandedPath);
        }

        // Add a root package that refs all additional dlls and sources the main dll

        var addedDeps = depsGroups.Select(d => new PackageDependencyGroup(d.TargetFramework,
            d.Packages.Concat(additionalDlls.Select(dll => new PackageDependency(dll)))));

        yield return new WorkspaceEntry(packageIdentity, sha256,
            addedDeps,
            FilterSpecificDll(localPackage.RuntimeItemGroups, mainDll),
            Array.Empty<FrameworkSpecificGroup>(),
            Array.Empty<FrameworkSpecificGroup>(), null, null,
            packageSource: source,
            expandedPath: localPackage.LocalPackageSourceInfo.Package.ExpandedPath);
      }
      else
      {
        yield return new WorkspaceEntry(packageIdentity, sha256,
            //  TODO For now we pass runtime as deps. This should be different elements in bazel tasks
            depsGroups, localPackage.RuntimeItemGroups ?? localPackage.RefItemGroups, Array.Empty<FrameworkSpecificGroup>(), Array.Empty<FrameworkSpecificGroup>(), null, null,
            packageSource: source,
            expandedPath: localPackage.LocalPackageSourceInfo.Package.ExpandedPath);
      }

      // TODO consider target framework
      IEnumerable<PackageDependency> RemoveEmptyDeps(PackageDependency dependency, NuGetFramework targetFramework)
      {
        if(SdkList.Dlls.Contains(dependency.Id.ToLower()) || !_localPackages.ContainsKey(dependency.Id)
                                                          || _localPackages[dependency.Id].RuntimeItemGroups.Any(g => g.Items.Any()))
        {
          return new[] { dependency };
        }

        var deps = _localPackages[dependency.Id]
          .LocalPackageSourceInfo.Package.Nuspec.GetDependencyGroups()
          .FirstOrDefault(g => g.TargetFramework == targetFramework);

        if(deps == null)
        {
          return Array.Empty<PackageDependency>();
        }

        return deps.Packages.SelectMany(p => RemoveEmptyDeps(p, targetFramework));
      }
    }

    private static IEnumerable<FrameworkSpecificGroup> FilterSpecificDll(IEnumerable<FrameworkSpecificGroup> groups, string dll) =>
        groups.Select(g => new FrameworkSpecificGroup(g.TargetFramework,
            g.Items.Where(f => string.Equals(Path.GetFileNameWithoutExtension(f), dll, StringComparison.OrdinalIgnoreCase))));
  }
}