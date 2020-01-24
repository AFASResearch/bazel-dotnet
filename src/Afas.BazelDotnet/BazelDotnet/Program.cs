using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Afas.BazelDotnet.Nuget;
using Microsoft.Extensions.CommandLineUtils;

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
      
      var workspace = app.Argument("workspace", "The path to the workspace root");

      app.OnExecute(async () =>
      {
        await GenerateDependencies(workspace.Value, "deps.bzl").ConfigureAwait(false);
        return 0;
      });

      app.Command("generate", command =>
      {
        command.Description = "List objects in the repositories";
        command.HelpOption("-?|-h|--help");

        command.Command("dependencies", repoCmd =>
        {
          command.HelpOption("-?|-h|--help");

          var output = app.Argument("output", "The output filename ending in .bzl");

          repoCmd.OnExecute(async () =>
          {
            await GenerateDependencies(workspace.Value, output.Value).ConfigureAwait(false);
            return 0;
          });
        });

        command.Command("projects", repoCmd =>
        {
          command.HelpOption("-?|-h|--help");
          repoCmd.OnExecute(async () =>
          {
            // TODO implement
            return 0;
          });
        });

        command.Command("workspace", repoCmd =>
        {
          command.HelpOption("-?|-h|--help");
          repoCmd.OnExecute(async () =>
          {
            // TODO implement
            return 0;
          });
        });
      });

      app.Execute(args);
    }

    private static async Task GenerateDependencies(string workspace, string output)
    {
      var packagesProps = XElement.Load(Path.Combine(workspace, "Packages.Props"));

      var deps = packagesProps
        .Element("ItemGroup")
        .Elements("PackageReference")
        .Select(el => (el.Attribute("Update")?.Value, el.Attribute("Version")?.Value))
        .Where(Included)
        // This one is upgrade due to the FrameworkReference in Afas.Cqrs
        .Append(("microsoft.aspnetcore.http.features", "3.1.0"))
        .ToArray();

      bool Included((string update, string version) arg) =>
        !string.IsNullOrEmpty(arg.update) &&
        !string.IsNullOrEmpty(arg.version) &&
        !arg.version.EndsWith("-local-dev", StringComparison.OrdinalIgnoreCase);

      var content = await new NugetDependencyFileGenerator(workspace, new AfasPackageSourceResolver())
        .Generate("netcoreapp3.1", "win", deps)
        .ConfigureAwait(false);

      File.WriteAllText(
        Path.Combine(workspace, output),
        $"load(\":nuget.bzl\", \"nuget_package\")\r\n\r\ndef deps():\r\n{content}");
    }
  }
}
