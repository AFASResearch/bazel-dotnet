using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Afas.BazelDotnet.Project
{
  public class CsProjBuildFileGenerator
  {
    // public void GenerateBuildFile(string projectFile)
    // {
    //   var definition = definition.Deserialize(_files, XDocument.Load(projectFilePath));
    //   var bazelDefinition = new BazelDefinitionBuilder(definition).Build();
    //   File.WriteAllText(Path.Combine(Path.GetDirectoryName(projectFile), "BUILD"), bazelDefinition.Serialize());
    // }
    //
    // private CsProjectFileDefinition FindAndParseProjectFile(string projectFilePath)
    // {
    //   var definition = new CsProjectFileDefinition(projectFilePath, solutionBasePath);
    //   return definition.Deserialize(_files, XDocument.Load(projectFilePath));
    // }
  }
}