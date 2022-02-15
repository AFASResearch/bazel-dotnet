using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.DependencyResolver;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.RuntimeModel;
using NuGet.Versioning;

namespace Afas.BazelDotnet.Nuget
{
  internal class RootProject
  {
    private const string _rootProjectName = "Root";
    private const string _rootProjectVersion = "1.0.0";
    private readonly Lazy<bool> _isLockFileValid;

    public RootProject(IReadOnlyCollection<FrameworkRuntimePair> targets, IReadOnlyCollection<LibraryRange> dependencies, PackagesLockFile nugetLockFile = null)
    {
      Targets = targets;
      NugetLockFile = nugetLockFile;
      PackageSpec = PrepareRootProject(targets, dependencies);
      _isLockFileValid = new Lazy<bool>(() =>
      {
        if(NugetLockFile == null)
        {
          return false;
        }

        DependencyGraphSpec spec = new DependencyGraphSpec().CreateFromClosure(_rootProjectName, new [] { PackageSpec });
        return PackagesLockFileUtilities.IsLockFileValid(spec, NugetLockFile).IsValid;
      });
    }

    public IReadOnlyCollection<FrameworkRuntimePair> Targets { get; }

    public PackageSpec PackageSpec { get; }

    public PackagesLockFile NugetLockFile { get; }

    public string Name => _rootProjectName;

    public string Version => _rootProjectVersion;

    public bool IsLockFileValid => _isLockFileValid.Value;

    private static PackageSpec PrepareRootProject(IReadOnlyCollection<FrameworkRuntimePair> targets, IReadOnlyCollection<LibraryRange> dependencies) =>
      new PackageSpec(targets.Select(t => new TargetFrameworkInformation { FrameworkName = t.Framework }).ToArray())
      {
        Name = _rootProjectName,
        Version = NuGetVersion.Parse(_rootProjectVersion),
        Dependencies = dependencies.Select(package => new LibraryDependency
          {
            LibraryRange = package,
            Type = LibraryDependencyType.Build,
            IncludeType = LibraryIncludeFlags.None,
            SuppressParent = LibraryIncludeFlags.All,
            AutoReferenced = true,
            GeneratePathProperty = false,
          })
          .ToList(),
        RestoreMetadata = new ProjectRestoreMetadata { ProjectUniqueName = _rootProjectName },
        RuntimeGraph = new RuntimeGraph(targets.Where(t => !string.IsNullOrEmpty(t.RuntimeIdentifier)).Select(t => new RuntimeDescription(t.RuntimeIdentifier)).Distinct())
      };
  }
}
