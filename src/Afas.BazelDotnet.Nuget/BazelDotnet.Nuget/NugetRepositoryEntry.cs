using System.Collections.Generic;
using NuGet.Packaging;
using NuGet.Repositories;
using NuGet.Versioning;

namespace Afas.BazelDotnet.Nuget
{
  internal class NugetRepositoryEntry
  {
    public NugetRepositoryEntry(
      LocalPackageSourceInfo localPackageSourceInfo,
      IReadOnlyCollection<FrameworkSpecificGroup> refItemGroups,
      IReadOnlyCollection<FrameworkSpecificGroup> runtimeItemGroups,
      IReadOnlyCollection<FrameworkSpecificGroup> contentFileGroups,
      IReadOnlyCollection<FrameworkSpecificGroup> analyzerItemGroups,
      IReadOnlyCollection<PackageDependencyGroup> dependencyGroups)
    {
      LocalPackageSourceInfo = localPackageSourceInfo;
      RefItemGroups = refItemGroups;
      RuntimeItemGroups = runtimeItemGroups;
      ContentFileGroups = contentFileGroups;
      AnalyzerItemGroups = analyzerItemGroups;
      DependencyGroups = dependencyGroups;
    }

    public LocalPackageSourceInfo LocalPackageSourceInfo { get; }

    public NuGetVersion Version => LocalPackageSourceInfo.Package.Version;

    public string Id => LocalPackageSourceInfo.Package.Id;

    public IReadOnlyCollection<FrameworkSpecificGroup> RefItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> RuntimeItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> ContentFileGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> AnalyzerItemGroups { get; }

    public IReadOnlyCollection<PackageDependencyGroup> DependencyGroups { get; }
  }
}