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
      IReadOnlyCollection<FrameworkSpecificGroup> toolItemGroups,
      IReadOnlyCollection<FrameworkSpecificGroup> analyzerItemGroups)
    {
      LocalPackageSourceInfo = localPackageSourceInfo;
      LibItemGroups = libItemGroups;
      RuntimeItemGroups = runtimeItemGroups;
      ToolItemGroups = toolItemGroups;
      AnalyzerItemGroups = analyzerItemGroups;
    }

    public LocalPackageSourceInfo LocalPackageSourceInfo { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> LibItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> RuntimeItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> ToolItemGroups { get; }

    public IReadOnlyCollection<FrameworkSpecificGroup> AnalyzerItemGroups { get; }
  }
}