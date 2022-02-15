using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Client;
using NuGet.ContentModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Repositories;

namespace Afas.BazelDotnet.Nuget
{
  internal class NugetRepositoryEntryBuilder
  {
    private readonly ManagedCodeConventions _conventions;
    private readonly IReadOnlyCollection<FrameworkRuntimePair> _targets;

    public NugetRepositoryEntryBuilder(ManagedCodeConventions conventions, IReadOnlyCollection<FrameworkRuntimePair> targets)
    {
      _conventions = conventions;
      _targets = targets;
    }

    public NugetRepositoryEntry ResolveGroups(LocalPackageSourceInfo localPackageSourceInfo)
    {
      var collection = new ContentItemCollection();
      collection.Load(localPackageSourceInfo.Package.Files);
      var allPackageDependencyGroups = localPackageSourceInfo.Package.Nuspec.GetDependencyGroups().ToArray();
      var frameworkReferenceGroups = localPackageSourceInfo.Package.Nuspec.GetFrameworkRefGroups().ToArray();

      var entry = new NugetRepositoryEntry(localPackageSourceInfo);

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

        // shipped debug binaries
        var netcoreappdebugAssemblies = new PatternSet(_conventions.Properties, new[]
        {
          new PatternDefinition("netcoreappdebug/{tfm}/{any?}"),
          new PatternDefinition("netcoreappdebug/{assembly?}")
        }, new[]
        {
          new PatternDefinition("netcoreappdebug/{tfm}/{assembly}"),
          new PatternDefinition("netcoreappdebug/{assembly}")
        });

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

        // "2.0.0/build/netstandard2.0/ref/netstandard.dll"
        var netstandardRefAssemblies = new PatternSet(_conventions.Properties, new []
        {
          new PatternDefinition("build/{tfm}/ref/{any?}"),
        }, new[]
        {
          new PatternDefinition("build/{tfm}/ref/{assembly}"),
        });

        AddIfNotNull(entry.RefItemGroups, target.Framework,
          collection.FindBestItemGroup(criteria,
            _conventions.Patterns.CompileRefAssemblies,
            netstandardRefAssemblies,
            _conventions.Patterns.CompileLibAssemblies)
          ?.Items);

        AddIfNotNull(entry.RuntimeItemGroups, target.Framework,
          collection.FindBestItemGroup(criteria,
            _conventions.Patterns.RuntimeAssemblies,
            _conventions.Patterns.NativeLibraries,
            buildAssemblies)
          ?.Items.Where(IsDll));

        AddIfNotNull(entry.DebugRuntimeItemGroups, target.Framework,
          collection.FindItemGroups(netcoreappdebugAssemblies)
            .SingleOrDefault()
          ?.Items.Where(IsDll));

        AddIfNotNull(entry.ContentFileGroups, target.Framework,
          collection.FindBestItemGroup(criteria,
            _conventions.Patterns.ContentFiles)
          ?.Items);

        AddIfNotNull(entry.AnalyzerItemGroups, target.Framework,
          collection.FindItemGroups(analyzerAssemblies)
            .SingleOrDefault()
          ?.Items);

        // Merge FrameworkReferences with normal PackageReferences
        var dependencies = NuGetFrameworkUtility.GetNearest(allPackageDependencyGroups, target.Framework);
        var frameworks = NuGetFrameworkUtility.GetNearest(frameworkReferenceGroups, target.Framework);

        if(dependencies != null || frameworks != null)
        {
          entry.DependencyGroups.Add(new PackageDependencyGroup(
            dependencies?.TargetFramework ?? frameworks?.TargetFramework,
            new[]
              {
                frameworks?.FrameworkReferences.Select(FrameworkDependencyResolver.ConvertToDependency),
                dependencies?.Packages,
              }
              .SelectMany(v => v ?? Array.Empty<PackageDependency>())));
        }
      }

      return entry;

      // Because Patterns.NativeLibraries matches any we sometimes contain pdb's
      // Constructing NativeLibraries ourselves is not possible due to some internal ManagedCodeConventions
      static bool IsDll(ContentItem item) => item.Path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

      void AddIfNotNull(ICollection<FrameworkSpecificGroup> c, NuGetFramework targetFramework, IEnumerable<ContentItem> items)
      {
        if(items != null)
        {
          c.Add(new FrameworkSpecificGroup(targetFramework, items.Select(i => i.Path)));
        }
      }
    }

    public NugetRepositoryEntry BuildFrameworkOverride(NugetRepositoryEntry entry, string frameworkOverride)
    {
      Console.WriteLine($"Overwriting {entry.LocalPackageSourceInfo.Package.Id}");
      var newEntry = new NugetRepositoryEntry(entry.LocalPackageSourceInfo);
      newEntry.DependencyGroups.AddRange(entry.DependencyGroups);
      if(frameworkOverride != null)
      {
        newEntry.RefItemGroups.Add(new FrameworkSpecificGroup(_targets.First().Framework, new []
        {
          frameworkOverride,
        }));
      }
      return newEntry;
    }
  }
}