using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Afas.BazelDotnet.Nuget;
using Afas.BazelDotnet.Project;
using McMaster.Extensions.CommandLineUtils;

namespace Afas.BazelDotnet
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var app = new CommandLineApplication
      {
        Name = "BazelDotnet",
        Description = "Bazel file generator for .NET Core projects",
      };

      app.HelpOption("-?|-h|--help");

      app.Command("dependencies", repoCmd =>
      {
        var packageProps = repoCmd.Argument("packageProps", "The path to the Packages.Props file");
        var nugetConfig = repoCmd.Argument("nugetConfig", "The path to the Packages.Props file");
        var depsBzl = repoCmd.Argument("depsBzl", "The output filename ending in .bzl");

        repoCmd.OnExecute(async () =>
        {
          var packagePropsFilePath = Path.Combine(Directory.GetCurrentDirectory(), packageProps.Value);
          var nugetConfigFilePath = Path.Combine(Directory.GetCurrentDirectory(), nugetConfig.Value);

          await GenerateDependencies(packagePropsFilePath, nugetConfigFilePath, depsBzl.Value).ConfigureAwait(false);
          return 0;
        });
      });

      app.Command("repository", repoCmd =>
      {
        var packageProps = repoCmd.Argument("packageProps", "The path to the Packages.Props file");
        var nugetConfig = repoCmd.Argument("nugetConfig", "The path to the Packages.Props file");

        repoCmd.OnExecute(async () =>
        {
          var packagePropsFilePath = Path.Combine(Directory.GetCurrentDirectory(), packageProps.Value);
          var nugetConfigFilePath = Path.Combine(Directory.GetCurrentDirectory(), nugetConfig.Value);

          await WriteRepository(packagePropsFilePath, nugetConfigFilePath).ConfigureAwait(false);
          return 0;
        });
      });

      app.Command("projects", repoCmd =>
      {
        var pathOption = repoCmd.Option("-p|--path", "The path to the workspace root", CommandOptionType.SingleOrNoValue);
        var workspaceOption = repoCmd.Option("-w|--workspace", "The workspace to load nugets from", CommandOptionType.SingleOrNoValue);
        
        repoCmd.HelpOption("-?|-h|--help");
        repoCmd.OnExecute(async () =>
        {
          string path;

          if(pathOption.HasValue())
          {
            path = pathOption.Value();
          }
          else
          {
            path = Environment.GetEnvironmentVariable("BUILD_WORKSPACE_DIRECTORY");

            if(string.IsNullOrEmpty(path))
            {
              throw new ArgumentException("Environment variable BUILD_WORKSPACE_DIRECTORY is missing");
            }
          }

          GenerateBuildFiles(path, workspaceOption.Value());
          return 0;
        });
      });
      
      if(!args.Any())
      {
        app.ShowHelp();
        throw new Exception("No arguments provided");
      }

      app.Execute(args);
    }

    private static (string, string)[] ResolvePackages(string packageProps)
    {
      if(File.Exists(packageProps))
      {
        var packagesProps = XElement.Load(packageProps);

        return packagesProps
          .Element("ItemGroup")
          .Elements("PackageReference")
          .Select(el => (el.Attribute("Update")?.Value, el.Attribute("Version")?.Value))
          .Where(Included)
          // This one is upgrade due to the FrameworkReference in Afas.Cqrs
          .Append(("microsoft.aspnetcore.http.features", "3.1.0"))
          .ToArray();
      }

      return Directory.EnumerateFiles(packageProps, "*.csproj", SearchOption.AllDirectories)
        .Select(XDocument.Load)
        .SelectMany(f => f.Descendants("PackageReference"))
        .Select(p => (p.Attribute("Include")?.Value, p.Attribute("Version")?.Value))
        .Where(t => t.Item1 != null && t.Item2 != null)
        .Distinct()
        .ToArray();

      bool Included((string update, string version) arg) =>
        !string.IsNullOrEmpty(arg.update) &&
        !string.IsNullOrEmpty(arg.version) &&
        !arg.version.EndsWith("-local-dev", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteRepository(string packageProps, string nugetConfig)
    {
      (string, string)[] deps = ResolvePackages(packageProps);

      await new NugetDependencyFileGenerator(nugetConfig, new AfasPackageSourceResolver())
        .WriteRepository("netcoreapp3.1", "win", deps)
        .ConfigureAwait(false);
    }

    private static async Task GenerateDependencies(string packageProps, string nugetConfig, string depsBzl)
    {
      (string, string)[] deps = ResolvePackages(packageProps);

      var content = await new NugetDependencyFileGenerator(nugetConfig, new AfasPackageSourceResolver())
        .GenerateDeps("netcoreapp3.1", "win", deps)
        .ConfigureAwait(false);

      File.WriteAllText(
        depsBzl,
        $"load(\":nuget.bzl\", \"nuget_package\")\r\n\r\ndef deps():\r\n{content}");
    }

    private static void GenerateBuildFiles(string workspace, string nugetWorkspace)
    {
      new CsProjBuildFileGenerator(workspace, nugetWorkspace).GlobAllProjects();
    }
  }
}