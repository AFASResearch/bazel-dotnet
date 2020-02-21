using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Afas.BazelDotnet.Project
{
  public class CsProjBuildFileGeneratorLegacy
  {
    private readonly string _workspace;
    private Dictionary<string, string> _files;

    public CsProjBuildFileGeneratorLegacy(string workspace)
    {
      _workspace = workspace;
    }

    public void GlobAllProjects(string extension = "csproj")
    {
      _files = Directory.EnumerateFiles(_workspace, $"*.{extension}", SearchOption.AllDirectories)
        // exclude bazel (tmp) folders
        .Where(p => !p.Contains("bazel-"))
        .ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);

      foreach(var projectFile in _files.Values)
      {
        var definition = FindAndParseProjectFile(_workspace, projectFile);
        var bazelDefinition = new BazelDefinitionBuilder(definition).Build();
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(projectFile), "BUILD"), bazelDefinition.Serialize());
      }
    }

    private CsProjectFileDefinition FindAndParseProjectFile(string solutionBasePath, string projectFilePath)
    {
      var definition = new CsProjectFileDefinition(projectFilePath, solutionBasePath);
      return definition.Deserialize(_files, XDocument.Load(projectFilePath));
    }
  }
}