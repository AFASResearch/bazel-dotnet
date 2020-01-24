using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Protocol.Core.Types;

namespace Afas.BazelDotnet.Nuget
{
  public class NugetDependencyFileGenerator
  {
    private readonly string _workspacePath;
    private readonly IPackageSourceResolver _packageSourceResolver;

    public NugetDependencyFileGenerator(string workspacePath, IPackageSourceResolver packageSourceResolver = null)
    {
      _workspacePath = workspacePath;
      _packageSourceResolver = packageSourceResolver;
    }

    public async Task<string> Generate(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      ILogger logger = new ConsoleLogger();
      var settings = Settings.LoadDefaultSettings(_workspacePath, null, new XPlatMachineWideSetting());
      
      // ~/.nuget/packages

      using(var cache = new SourceCacheContext())
      {
        var dependencyGraphResolver = new TransitiveDependencyResolver(settings, logger, cache);

        foreach((string package, string version) v in packageReferences)
        {
          dependencyGraphResolver.AddPackageReference(v.package, v.version);
        }
        
        var dependencyGraph = await dependencyGraphResolver.ResolveGraph(targetFramework, targetRuntime).ConfigureAwait(false);
        var localPackages = await dependencyGraphResolver.ResolveLocalPackages(dependencyGraph).ConfigureAwait(false);

        var workspaceEntryBuilder = new WorkspaceEntryBuilder(dependencyGraph.Conventions, _packageSourceResolver)
          .WithTarget(new FrameworkRuntimePair(NuGetFramework.Parse(targetFramework), targetRuntime));

        // First resolve al file groups
        var resolved = localPackages.Select(workspaceEntryBuilder.ResolveGroups).ToArray();

        // Then we use them to validate deps actually contain content
        workspaceEntryBuilder.WithLocalPackages(resolved);

        var entries = resolved.SelectMany(workspaceEntryBuilder.Build)
          .Where(entry => !SdkList.Dlls.Contains(entry.PackageIdentity.Id.ToLower()))
          .Select(entry => entry.Generate(indent: true));

        return string.Join(string.Empty, entries);
      }
    }
  }
}