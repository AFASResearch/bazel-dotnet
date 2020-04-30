using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Afas.BazelDotnet.Project
{
  public class CsProjBuildFileGenerator
  {
    private readonly string _workspace;
    private readonly string _nugetWorkspace;
    private readonly IReadOnlyDictionary<string, string> _importLabels;
    private IReadOnlyDictionary<string, string> _projectLabels;

    public CsProjBuildFileGenerator(string workspace, string nugetWorkspace, IReadOnlyDictionary<string, string> imports)
    {
      _workspace = workspace;
      _nugetWorkspace = nugetWorkspace;
      _importLabels = imports;
    }

    public void GlobAllProjects(IReadOnlyCollection<string> searchFolders = null, string extension = "csproj", string exportsFileName = null)
    {
      var filesEnum = searchFolders?.Any() == true ? searchFolders.SelectMany(f =>
          Directory.EnumerateFiles(Path.Combine(_workspace, f), $"*.{extension}", SearchOption.AllDirectories))
        : Directory.EnumerateFiles(_workspace, $"*.{extension}", SearchOption.AllDirectories);

      var files = filesEnum
        // exclude bazel (tmp) folders
        .Where(p => !p.Contains("bazel-"))
        .ToArray();

      _projectLabels = files.ToDictionary(Path.GetFileNameWithoutExtension, ToLabel, StringComparer.OrdinalIgnoreCase);

      foreach(var projectFile in files)
      {
        var definition = FindAndParseProjectFile(_workspace, projectFile);
        var bazelDefinition = new BazelDefinitionBuilder(definition, _nugetWorkspace).Build();

        var file = Path.Combine(Path.GetDirectoryName(projectFile), "BUILD");
        using(var stream = new StreamWriter(File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
        {
          stream.Write(bazelDefinition.Serialize());
        }
      }

      if(!string.IsNullOrEmpty(exportsFileName))
      {
        var values = _projectLabels.Select(l => $"{l.Key}={l.Value}");
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
      return definition.Deserialize(_projectLabels, _importLabels, XDocument.Load(projectFilePath));
    }
  }
}