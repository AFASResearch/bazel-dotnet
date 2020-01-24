using System.Collections.Generic;
using NuGet.Packaging;
using NuGet.Repositories;

namespace Afas.BazelDotnet.Nuget
{
  internal class LocalPackageWithGroups
  {
    public LocalPackageWithGroups(LocalPackageSourceInfo localPackageSourceInfo,
      IReadOnlyCollection<FrameworkSpecificGroup> libItemGroups,
      IReadOnlyCollection<FrameworkSpecificGroup> runtimeItemGroups,
      IReadOnlyCollection<FrameworkSpecificGroup> toolItemGroups)
    {
      LocalPackageSourceInfo = localPackageSourceInfo;
      LibItemGroups = libItemGroups;
      RuntimeItemGroups = runtimeItemGroups;
      ToolItemGroups = toolItemGroups;
    }

    public LocalPackageSourceInfo LocalPackageSourceInfo { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> LibItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> RuntimeItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> ToolItemGroups { get; }
  }
}