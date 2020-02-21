using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Afas.BazelDotnet.Project
{
  internal class CsProjectFileDefinition
  {
    public CsProjectFileDefinition(string projectFilePath)
    {
      RelativeFilePath = projectFilePath;
      PackageReferences = new List<string>();
      ProjectReference = new List<string>();
      EmbeddedResources = new List<EmbeddedResourceDefinition>();
      CopyToOutput = new List<string>();
    }

    public string RelativeFilePath { get; }

    public ProjectType Type { get; private set; }

    public List<string> PackageReferences { get; }

    public List<string> ProjectReference { get; }

    public List<EmbeddedResourceDefinition> EmbeddedResources { get; }

    public List<string> CopyToOutput { get; }

    public CsProjectFileDefinition Deserialize(Dictionary<string, string> projectFiles, XDocument document)
    {
      Type = GetProjectType(document);

      foreach(var reference in document.Descendants("PackageReference"))
      {
        var name = reference.Attribute("Include").Value;

        if(name.StartsWith("Afas.Generator", StringComparison.OrdinalIgnoreCase) || projectFiles.ContainsKey(name))
        {
          AddProjectReference(projectFiles[name]);
        }
        else
        {
          if(name.Equals("microsoft.net.test.sdk", StringComparison.OrdinalIgnoreCase))
          {
            PackageReferences.Add("microsoft.testplatform.testhost");
            PackageReferences.Add("microsoft.codecoverage");
          }
          else
          {
            PackageReferences.Add(name);
          }
        }
      }

      foreach(var descendant in document.Descendants("ProjectReference"))
      {
        var include = descendant.Attribute("Include").Value;
        AddProjectReference(Path.Combine(Path.GetDirectoryName(RelativeFilePath), include));
      }

      foreach(var resource in document.Descendants("EmbeddedResource"))
      {
        var include = resource.Attribute("Include")?.Value;
        var remove = resource.Attribute("Remove")?.Value;
        var update = resource.Attribute("Update")?.Value;

        if(include != null)
        {
          EmbeddedResources.Add(new EmbeddedResourceDefinition(EmbeddedResourceType.Include, include));
        }
        if(remove != null)
        {
          EmbeddedResources.Add(new EmbeddedResourceDefinition(EmbeddedResourceType.Remove, remove));
        }
        if(update != null)
        {
          EmbeddedResources.Add(new EmbeddedResourceDefinition(EmbeddedResourceType.Update, update));
        }
      }

      foreach(var copyNode in document.Descendants("CopyToOutputDirectory"))
      {
        // PreserveNewest ?

        var include = copyNode.Parent?.Attribute("Include")?.Value
                   ?? copyNode.Parent?.Attribute("Update")?.Value;
        if(include != null)
        {
          CopyToOutput.Add(include.Replace("\\", "/"));
        }
      }

      return this;
    }

    private void AddProjectReference(string csprojFilePath)
    {
      var name = Path.GetDirectoryName(
          // Ugly but works. c# does not seem to provide a Path.Normalize to rid of ../
          Path.GetRelativePath(
            Directory.GetCurrentDirectory(),
            Path.GetFullPath(csprojFilePath)))
        .Replace('\\', '/');

      ProjectReference.Add($"//{name}:{Path.GetFileNameWithoutExtension(csprojFilePath)}");
    }

    private ProjectType GetProjectType(XDocument document)
    {
      var outputType = document.Descendants("OutputType").FirstOrDefault();
      if(outputType != null && outputType.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase))
      {
        return ProjectType.Binary;
      }

      if(RelativeFilePath.EndsWith("Test.csproj") || RelativeFilePath.EndsWith("Tests.csproj"))
      {
        return ProjectType.Test;
      }

      return ProjectType.Library;
    }
  }
}