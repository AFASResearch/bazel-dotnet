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
    private Dictionary<string, string> _files;

    public CsProjBuildFileGenerator(string workspace, string nugetWorkspace)
    {
      _workspace = workspace;
      _nugetWorkspace = nugetWorkspace;
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
        var bazelDefinition = new BazelDefinitionBuilder(definition, _nugetWorkspace).Build();

        var file = Path.Combine(Path.GetDirectoryName(projectFile), "BUILD");
        using(var stream = new StreamWriter(File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
        {
          stream.Write(bazelDefinition.Serialize());
        }
      }
    }

    private CsProjectFileDefinition FindAndParseProjectFile(string solutionBasePath, string projectFilePath)
    {
      var definition = new CsProjectFileDefinition(projectFilePath, solutionBasePath);
      return definition.Deserialize(_files, XDocument.Load(projectFilePath));
    }
  }
}