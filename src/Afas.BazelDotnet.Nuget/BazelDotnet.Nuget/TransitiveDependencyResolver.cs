using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.DependencyResolver;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGet.RuntimeModel;
using NuGet.Versioning;

#nullable enable

namespace Afas.BazelDotnet.Nuget
{
  internal class TransitiveDependencyResolver
  {
    private readonly RootProject _rootProject;
    private readonly RemoteWalkContext _context;
    private readonly LocalPackageExtractor _localPackageExtractor;
    private readonly NuGetv3LocalRepository _v3LocalRepository;

    public TransitiveDependencyResolver(ISettings settings, ILogger logger, SourceCacheContext cache, RootProject rootProject)
    {
      _rootProject = rootProject;
      _context = CreateRemoteWalkContext(settings, cache, logger, rootProject);
      _v3LocalRepository = new NuGetv3LocalRepository(SettingsUtility.GetGlobalPackagesFolder(settings), new LocalPackageFileCache(), false);
      _localPackageExtractor = new LocalPackageExtractor(settings, _v3LocalRepository, _context.Logger, _context.CacheContext);
    }

    public async Task<RestoreTargetGraph> ResolveFrameworkReference(string id, string version, string targetFramework)
    {
      var nugetTargetFramework = NuGetFramework.Parse(targetFramework);
      return await GetIndependentGraph(id, version, nugetTargetFramework, _context).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RestoreTargetGraph>> ResolveGraphs()
    {
      var graphs = new List<RestoreTargetGraph>();

      foreach(var target in _rootProject.Targets)
      {
        var independentGraph = await GetIndependentGraph(_rootProject.Name, _rootProject.Version, target.Framework, _context).ConfigureAwait(false);

        graphs.Add(independentGraph);

        if(string.IsNullOrEmpty(target.RuntimeIdentifier))
        {
          continue;
        }

        // We could target multiple runtimes with RuntimeGraph.Merge
        var platformSpecificGraph = await GetPlatformSpecificGraph(independentGraph, _rootProject.Name, _rootProject.Version,
            target.Framework, target.RuntimeIdentifier, _context, _localPackageExtractor)
          .ConfigureAwait(false);

        graphs.Add(platformSpecificGraph);
      }

      return graphs;
    }

    public async Task<IReadOnlyCollection<LocalPackageSourceInfo>> DownloadPackages(IEnumerable<RestoreTargetGraph> dependencyGraphs)
    {
      return await Task.WhenAll(dependencyGraphs.SelectMany(g => g.Flattened)
        .Distinct(GraphItemKeyComparer<RemoteResolveResult>.Instance)
        .Where(i => !string.Equals(i.Key.Name, _rootProject.Name, StringComparison.OrdinalIgnoreCase))
        .Select(DownloadPackage));
    }

    public Task<LocalPackageSourceInfo> DownloadPackage(GraphItem<RemoteResolveResult> item) =>
      _localPackageExtractor.EnsureLocalPackage(item.Data.Match.Provider, ToPackageIdentity(item.Data.Match));

    public string RenderLockFile(IEnumerable<RestoreTargetGraph> targetGraphs)
    {
      var builder = new LockFileBuilder(lockFileVersion: 2, _context.Logger, new());
      var lockFile = builder.CreateLockFile(previousLockFile: new LockFile(), _rootProject.PackageSpec, targetGraphs, new [] { _v3LocalRepository }, _context);
      var nugetLockFile = new PackagesLockFileBuilder().CreateNuGetLockFile(lockFile);
      return PackagesLockFileFormat.Render(nugetLockFile);
    }

    private static RemoteWalkContext CreateRemoteWalkContext(ISettings settings, SourceCacheContext cache, ILogger logger, RootProject rootProject)
    {
      // nuget.org etc.
      var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);
      var localRepository = Repository.Factory.GetCoreV3(globalPackagesFolder, FeedType.FileSystemV3);
      var sourceRepositoryProvider = new SourceRepositoryProvider(new PackageSourceProvider(settings), Repository.Provider.GetCoreV3());

      var context = new RemoteWalkContext(cache, logger);

      context.LocalLibraryProviders.Add(new SourceRepositoryDependencyProvider(localRepository, logger, cache,
        ignoreFailedSources: true,
        fileCache: new LocalPackageFileCache(),
        ignoreWarning: true,
        isFallbackFolderSource: false));

      foreach(var remoteRepository in sourceRepositoryProvider.GetRepositories())
      {
        context.RemoteLibraryProviders.Add(new SourceRepositoryDependencyProvider(remoteRepository, logger, cache,
          ignoreFailedSources: cache.IgnoreFailedSources,
          fileCache: new LocalPackageFileCache(),
          ignoreWarning: false,
          isFallbackFolderSource: false));
      }

      context.ProjectLibraryProviders.Add(new PackageSpecReferenceDependencyProvider(new[]
      {
        new ExternalProjectReference(
          rootProject.Name,
          rootProject.PackageSpec,
          msbuildProjectPath: null,
          projectReferences: Enumerable.Empty<string>()),
      }, context.Logger));

      if(rootProject.IsLockFileValid)
      {
        // pass lock file details down to generate restore graph
        foreach (var target in rootProject.NugetLockFile.Targets)
        {
          var libraries = target.Dependencies
            .Where(dep => dep.Type != PackageDependencyType.Project)
            .Select(dep => new LibraryIdentity(dep.Id, dep.ResolvedVersion, LibraryType.Package))
            .ToList();

          // TODO clean up
          libraries.Add(new LibraryIdentity("Microsoft.NETCore.App.Ref", new NuGetVersion(target.TargetFramework.Version), LibraryType.Package));
          libraries.Add(new LibraryIdentity("Microsoft.AspNetCore.App.Ref", new NuGetVersion(target.TargetFramework.Version), LibraryType.Package));

          // add lock file libraries into RemoteWalkContext so that it can be used during restore graph generation
          context.LockFileLibraries.Add(new LockFileCacheKey(target.TargetFramework, target.RuntimeIdentifier), libraries);
        }
      }

      return context;
    }

    private async Task<RestoreTargetGraph> GetIndependentGraph(string package, string version, NuGetFramework nuGetFramework, RemoteWalkContext context)
    {
      var result = await new RemoteDependencyWalker(context).WalkAsync(
        new LibraryRange(package, VersionRange.Parse(version), LibraryDependencyTarget.All),
        nuGetFramework,
        runtimeGraph: RuntimeGraph.Empty,
        recursive: true,
        runtimeIdentifier: null);

      return RestoreTargetGraph.Create(RuntimeGraph.Empty, new[]
      {
        result
      }, _context, _context.Logger, nuGetFramework, runtimeIdentifier: null);
    }

    private static async Task<RestoreTargetGraph> GetPlatformSpecificGraph(RestoreTargetGraph independentGraph,
      string package, string version, NuGetFramework framework, string runtimeIdentifier,
      RemoteWalkContext context, LocalPackageExtractor extractor)
    {
      var graphTask = independentGraph.Flattened
        .Where(m => m.Data?.Match?.Library?.Type == LibraryType.Package)
        .Select(GetRuntimeGraphTask);

      var graphs = (await Task.WhenAll(graphTask))
        .Where(i => i != null);

      var runtimeGraph = graphs.Aggregate(RuntimeGraph.Empty, RuntimeGraph.Merge);

      // This results in additional entries
      var resultWin = await new RemoteDependencyWalker(context).WalkAsync(
        new LibraryRange(package, VersionRange.Parse(version), LibraryDependencyTarget.All),
        framework,
        runtimeGraph: runtimeGraph,
        recursive: true,
        runtimeIdentifier: runtimeIdentifier);

      return RestoreTargetGraph.Create(runtimeGraph, new[]
      {
        resultWin
      }, context, context.Logger, framework, runtimeIdentifier);

      async Task<RuntimeGraph?> GetRuntimeGraphTask(GraphItem<RemoteResolveResult> item)
      {
        var packageIdentity = ToPackageIdentity(item.Data.Match);

        var localPackageSourceInfo = await extractor.EnsureLocalPackage(item.Data.Match.Provider, packageIdentity);

        return localPackageSourceInfo?.Package?.RuntimeGraph;
      }
    }

    private static PackageIdentity ToPackageIdentity(RemoteMatch match)
    {
      return new PackageIdentity(match.Library.Name, match.Library.Version);
    }
  }
}