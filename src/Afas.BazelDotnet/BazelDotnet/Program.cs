using System;
using System.Collections.Generic;
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

      app.Command("repository", repoCmd =>
      {
        var nugetConfig = repoCmd.Argument("nugetConfig", "The path to the Packages.Props file");
        var packageProps = repoCmd.Option("-p|--package", "Packages.Props files", CommandOptionType.MultipleValue);

        repoCmd.OnExecute(async () =>
        {
          var packagePropsFilePaths = packageProps.Values.Select(v => Path.Combine(Directory.GetCurrentDirectory(), v)).ToArray();
          var nugetConfigFilePath = Path.Combine(Directory.GetCurrentDirectory(), nugetConfig.Value);

          await WriteRepository(packagePropsFilePaths, nugetConfigFilePath).ConfigureAwait(false);
          return 0;
        });
      });

      app.Command("projects", repoCmd =>
      {
        var pathOption = repoCmd.Option("-p|--path", "The path to the workspace root", CommandOptionType.SingleOrNoValue);
        var workspaceOption = repoCmd.Option("-w|--workspace", "The workspace to load nugets from", CommandOptionType.SingleOrNoValue);
        var exportsOption = repoCmd.Option("-e|--exports", "Exports file with dictionary of provided project labels (PackageName=Label)", CommandOptionType.SingleOrNoValue);
        var importsOption = repoCmd.Option("-i|--imports", "Import files with dictionary of imported project labels (PackageName=Label)", CommandOptionType.MultipleValue);
        var searchOption = repoCmd.Option("--search", "Specify folders to search", CommandOptionType.MultipleValue);
        
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

          GenerateBuildFiles(path, workspaceOption.Value(), exportsOption.Value(),  importsOption.Values, searchOption.Values);
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

    private static async Task WriteRepository(IEnumerable<string> packagePropsFiles, string nugetConfig)
    {
      // TODO conlict resolution. Maybe we can add them in the dep graph
      (string, string)[] deps = packagePropsFiles
        .SelectMany(ResolvePackages)
        .Distinct()
        .ToArray();

      await new NugetRepositoryGenerator(nugetConfig)
        .WriteRepository("netcoreapp3.1", "win-x64", deps)
        .ConfigureAwait(false);
    }

    private static void GenerateBuildFiles(string workspace, string nugetWorkspace, string exportsFileName = null,
      IReadOnlyCollection<string> importMappings = null, IReadOnlyCollection<string> searchFolders = null)
    {
      importMappings ??= Array.Empty<string>();

      IEnumerable<(string, string)> ReadLines(string repoName, string fileName) =>
        File.ReadAllLines(fileName)
          .Select(l => l.Trim())
          .Where(l => !string.IsNullOrEmpty(l))
          // Project=//src/Project:Project
          .Select(l => l.Split("="))
          // (Project, @projects//src/Project:Project)
          .Select(l => (l[0], $"{repoName}{l[1]}"));

      // Mappings of import files
      var imports = importMappings
        // @projects=C:/bazel/external/projects/projects
        .Select(m => m.Split("="))
        .SelectMany(s => ReadLines(s[0], s[1]))
        .ToDictionary(t => t.Item1, t => t.Item2);

      new CsProjBuildFileGenerator(workspace, nugetWorkspace, imports).GlobAllProjects(searchFolders, exportsFileName: exportsFileName);
    }
  }
}