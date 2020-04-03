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
    private readonly List<FrameworkRuntimePair> _targets;

    public WorkspaceEntryBuilder(ManagedCodeConventions conventions)
    {
      _conventions = conventions;
      _targets = new List<FrameworkRuntimePair>();
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
  }
}