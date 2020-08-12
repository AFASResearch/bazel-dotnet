using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Client;
using NuGet.ContentModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Repositories;

namespace Afas.BazelDotnet.Nuget
{
  internal class NugetRepositoryEntryBuilder
  {
    private readonly ManagedCodeConventions _conventions;
    private readonly List<FrameworkRuntimePair> _targets;

    public NugetRepositoryEntryBuilder(ManagedCodeConventions conventions)
    {
      _conventions = conventions;
      _targets = new List<FrameworkRuntimePair>();
    }

    public NugetRepositoryEntryBuilder WithTarget(FrameworkRuntimePair target)
    {
      _targets.Add(target);
      return this;
    }

    public NugetRepositoryEntryBuilder WithTarget(NuGetFramework target)
    {
      _targets.Add(new FrameworkRuntimePair(target, runtimeIdentifier: null));
      return this;
    }

    public NugetRepositoryEntry ResolveGroups(LocalPackageSourceInfo localPackageSourceInfo)
    {
      var collection = new ContentItemCollection();
      collection.Load(localPackageSourceInfo.Package.Files);
      var allPackageDependencyGroups = localPackageSourceInfo.Package.Nuspec.GetDependencyGroups().ToArray();

      var refItemGroups = new List<FrameworkSpecificGroup>();
      var runtimeItemGroups = new List<FrameworkSpecificGroup>();
      var analyzerItemGroups = new List<FrameworkSpecificGroup>();
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
          _conventions.Patterns.NativeLibraries,
          buildAssemblies);

        // The analyzer dll's are published in analyzers/ or analyzers/dotnet/cs/
        var analyzerAssemblies = new PatternSet(_conventions.Properties, new []
        {
          new PatternDefinition("analyzers/dotnet/cs/{assembly?}"),
          new PatternDefinition("analyzers/{assembly?}")
        }, new []
        {
          new PatternDefinition("analyzers/dotnet/cs/{assembly}"),
          new PatternDefinition("analyzers/{assembly}")
        });
        var bestAnalyzerGroup = collection.FindItemGroups(analyzerAssemblies).SingleOrDefault();

        if(bestRefGroup != null)
        {
          refItemGroups.Add(new FrameworkSpecificGroup(target.Framework, bestRefGroup.Items.Select(i => i.Path)));
        }

        if(bestRuntimeGroup != null)
        {
          runtimeItemGroups.Add(new FrameworkSpecificGroup(target.Framework, bestRuntimeGroup.Items.Where(IsDll).Select(i => i.Path)));

          // Because Patterns.NativeLibraries matches any we sometimes contain pdb's
          // Constructing NativeLibraries ourselves is not possible due to some internal ManagedCodeConventions
          static bool IsDll(ContentItem item) => item.Path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        }

        if(bestAnalyzerGroup != null)
        {
          analyzerItemGroups.Add(new FrameworkSpecificGroup(target.Framework, bestAnalyzerGroup.Items.Select(i => i.Path)));
        }

        dependencyGroups.Add(NuGetFrameworkUtility.GetNearest(allPackageDependencyGroups, target.Framework));
      }

      return new NugetRepositoryEntry(localPackageSourceInfo, refItemGroups, runtimeItemGroups, analyzerItemGroups, dependencyGroups);
    }

    public NugetRepositoryEntry BuildFrameworkOverride(NugetRepositoryEntry entry, string frameworkOverride)
    {
      Console.WriteLine($"Overwriting {entry.LocalPackageSourceInfo.Package.Id}");
      return new NugetRepositoryEntry(entry.LocalPackageSourceInfo, frameworkOverride == null ? Array.Empty<FrameworkSpecificGroup>() : new[]
      {
        new FrameworkSpecificGroup(_targets.Single().Framework, new []
        {
          frameworkOverride,
        }),
      }, Array.Empty<FrameworkSpecificGroup>(), Array.Empty<FrameworkSpecificGroup>(), entry.DependencyGroups);
    }
  }
}