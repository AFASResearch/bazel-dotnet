using System;
using System.Collections.Generic;
using NuGet.Packaging;
using NuGet.Repositories;
using NuGet.Versioning;

namespace Afas.BazelDotnet.Nuget
{
  internal class NugetRepositoryEntry
  {
    public NugetRepositoryEntry(LocalPackageSourceInfo localPackageSourceInfo,
      List<FrameworkSpecificGroup> refItemGroups = null,
      List<FrameworkSpecificGroup> runtimeItemGroups = null,
      List<FrameworkSpecificGroup> debugRuntimeItemGroups = null,
      List<FrameworkSpecificGroup> contentFileGroups = null,
      List<FrameworkSpecificGroup> analyzerItemGroups = null,
      List<PackageDependencyGroup> dependencyGroups = null)
    {
      LocalPackageSourceInfo = localPackageSourceInfo;
      RefItemGroups = refItemGroups ?? new List<FrameworkSpecificGroup>();
      RuntimeItemGroups = runtimeItemGroups ?? new List<FrameworkSpecificGroup>();
      DebugRuntimeItemGroups = debugRuntimeItemGroups ?? new List<FrameworkSpecificGroup>();
      ContentFileGroups = contentFileGroups ?? new List<FrameworkSpecificGroup>();
      AnalyzerItemGroups = analyzerItemGroups ?? new List<FrameworkSpecificGroup>();
      DependencyGroups = dependencyGroups ?? new List<PackageDependencyGroup>();
    }

    public LocalPackageSourceInfo LocalPackageSourceInfo { get; }

    public NuGetVersion Version => LocalPackageSourceInfo.Package.Version;

    public string Id => LocalPackageSourceInfo.Package.Id;

    public List<FrameworkSpecificGroup> RefItemGroups { get; }

    public List<FrameworkSpecificGroup> RuntimeItemGroups { get; }

    public List<FrameworkSpecificGroup> DebugRuntimeItemGroups { get; }

    public List<FrameworkSpecificGroup> ContentFileGroups { get; }

    public List<FrameworkSpecificGroup> AnalyzerItemGroups { get; }

    public List<PackageDependencyGroup> DependencyGroups { get; }
  }
}