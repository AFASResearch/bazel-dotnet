using System.Collections.Generic;
using NuGet.Packaging;
using NuGet.Repositories;

namespace Afas.BazelDotnet.Nuget
{
  internal class NugetRepositoryEntry
  {
    public NugetRepositoryEntry(LocalPackageSourceInfo localPackageSourceInfo,
      IReadOnlyCollection<FrameworkSpecificGroup> refItemGroups,
      IReadOnlyCollection<FrameworkSpecificGroup> runtimeItemGroups,
      IReadOnlyCollection<PackageDependencyGroup> dependencyGroups)
    {
      LocalPackageSourceInfo = localPackageSourceInfo;
      RefItemGroups = refItemGroups;
      RuntimeItemGroups = runtimeItemGroups;
      DependencyGroups = dependencyGroups;
    }

    public LocalPackageSourceInfo LocalPackageSourceInfo { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> RefItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> RuntimeItemGroups { get; }

    public IReadOnlyCollection<PackageDependencyGroup> DependencyGroups { get; }
  }
}