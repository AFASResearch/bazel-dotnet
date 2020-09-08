using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Afas.BazelDotnet.Project
{
  public class CsProjBuildFileGenerator
  {
    private readonly string _workspace;
    private readonly string _nugetWorkspace;
    private readonly IReadOnlyDictionary<string, string> _importLabels;

    public CsProjBuildFileGenerator(string workspace, string nugetWorkspace, IReadOnlyDictionary<string, string> imports)
    {
      _workspace = workspace;
      _nugetWorkspace = nugetWorkspace;
      _importLabels = imports;
    }

    private static void WriteFileIfChanged(string path, string newContents)
    {
      using var buildFileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
      using var reader = new StreamReader(buildFileStream);

      // only write to the file when we have changes so we do not introduce git diff's
      if(!string.Equals(newContents, reader.ReadToEnd(), StringComparison.OrdinalIgnoreCase))
      {
        buildFileStream.SetLength(0);
        using var writer = new StreamWriter(buildFileStream);
        writer.Write(newContents);
      }
    }

    public async Task GlobAllProjects(IReadOnlyCollection<string> searchFolders = null, string extension = "csproj", string exportsFileName = null)
    {
      var filesEnum = searchFolders?.Any() == true ? searchFolders.SelectMany(f =>
          Directory.EnumerateFiles(Path.Combine(_workspace, f), $"*.{extension}", SearchOption.AllDirectories))
        : Directory.EnumerateFiles(_workspace, $"*.{extension}", SearchOption.AllDirectories);

      var files = filesEnum
        // exclude bazel (tmp) folders
        .Where(p => !p.Contains("bazel-"))
        .ToArray();

      await Task.WhenAll(files.Select(projectFile => Task.Run(() =>
      {
        var definition = FindAndParseProjectFile(_workspace, projectFile);

        // only write to the file when we have changes so we do not introduce git diff's
        WriteFileIfChanged(
          Path.Combine(Path.GetDirectoryName(projectFile), "BUILD"),
          new BazelDefinitionBuilder(definition, _nugetWorkspace).Build().Serialize());
      }))).ConfigureAwait(false);

      if(!string.IsNullOrEmpty(exportsFileName))
      {
        var values = files
          .ToDictionary(Path.GetFileNameWithoutExtension, ToLabel, StringComparer.OrdinalIgnoreCase)
          .Select(l => $"{l.Key}={l.Value}");
        File.WriteAllText(exportsFileName, $"{string.Join("\n", values)}");
      }
    }

    private string ToLabel(string csprojFilePath)
    {
      var name = Path.GetDirectoryName(
          Path.GetRelativePath(_workspace, csprojFilePath))
        .Replace('\\', '/');

      return $"//{name}:{Path.GetFileNameWithoutExtension(csprojFilePath)}";
    }

    private CsProjectFileDefinition FindAndParseProjectFile(string solutionBasePath, string projectFilePath)
    {
      var definition = new CsProjectFileDefinition(projectFilePath, solutionBasePath);
      return definition.Deserialize(ToLabel, _importLabels, projectFilePath);
    }
  }
}