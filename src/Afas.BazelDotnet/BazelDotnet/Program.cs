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

      app.Command("projects", repoCmd =>
      {
        var slnBasePath = repoCmd.Argument("slnBasePath", "The path to the base of the solution");
        repoCmd.HelpOption("-?|-h|--help");
        repoCmd.OnExecute(async () =>
        {
          GenerateBuildFiles(slnBasePath.Value);
          return 0;
        });
      });

      // app.Command("project", repoCmd =>
      // {
      //   var csProjFilePath = repoCmd.Argument("csProjFilePath", "The path to the .csproj file");
      //   repoCmd.HelpOption("-?|-h|--help");
      //   repoCmd.OnExecute(async () =>
      //   {
      //     GenerateBuildFiles(csProjFilePath.Value);
      //     return 0;
      //   });
      // });

      if(!args.Any())
      {
        app.ShowHelp();
        throw new Exception("No arguments provided");
      }

      app.Execute(args);
    }

    private static async Task GenerateDependencies(string packageProps, string nugetConfig, string depsBzl)
    {
      (string, string)[] deps;

      if(File.Exists(packageProps))
      {
        var packagesProps = XElement.Load(packageProps);

        deps = packagesProps
          .Element("ItemGroup")
          .Elements("PackageReference")
          .Select(el => (el.Attribute("Update")?.Value, el.Attribute("Version")?.Value))
          .Where(Included)
          // This one is upgrade due to the FrameworkReference in Afas.Cqrs
          .Append(("microsoft.aspnetcore.http.features", "3.1.0"))
          .ToArray();
      }
      else
      {
        deps = Directory.EnumerateFiles(packageProps, "*.csproj", SearchOption.AllDirectories)
          .Select(XDocument.Load)
          .SelectMany(f => f.Descendants("PackageReference"))
          .Select(p => (p.Attribute("Include")?.Value, p.Attribute("Version")?.Value))
          .Where(t => t.Item1 != null && t.Item2 != null)
          .Distinct()
          .ToArray();
      }

      var content = await new NugetDependencyFileGenerator(nugetConfig, new AfasPackageSourceResolver())
        .Generate("netcoreapp3.1", "win", deps)
        .ConfigureAwait(false);

      File.WriteAllText(
        depsBzl,
        $"load(\":nuget.bzl\", \"nuget_package\")\r\n\r\ndef deps():\r\n{content}");

      bool Included((string update, string version) arg) =>
        !string.IsNullOrEmpty(arg.update) &&
        !string.IsNullOrEmpty(arg.version) &&
        !arg.version.EndsWith("-local-dev", StringComparison.OrdinalIgnoreCase);
    }

    private static void GenerateBuildFiles(string workspace)
    {
      new CsProjBuildFileGeneratorLegacy(workspace).GlobAllProjects();
    }
  }
}