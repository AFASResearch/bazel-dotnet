using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Afas.BazelDotnet.Project
{
  public class CsProjBuildFileGenerator
  {
    private readonly Dictionary<string, string> _projects;

    public CsProjBuildFileGenerator(List<string> projects)
    {
      _projects = projects.ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
    }

    public void GlobAllProjects()
    {
      foreach(var projectFile in _projects.Values)
      {
        var definition = FindAndParseProjectFile(projectFile);
        var bazelDefinition = new BazelDefinitionBuilder(definition).Build();
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(projectFile), "BUILD"), bazelDefinition.Serialize());
      }
    }

    private CsProjectFileDefinition FindAndParseProjectFile(string projectFilePath)
    {
      var definition = new CsProjectFileDefinition(projectFilePath);
      return definition.Deserialize(_projects, XDocument.Load(projectFilePath));
    }
  }
}