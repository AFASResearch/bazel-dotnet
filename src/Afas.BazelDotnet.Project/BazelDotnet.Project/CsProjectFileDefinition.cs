using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Afas.BazelDotnet.Project
{
  internal class CsProjectFileDefinition
  {
    private string _slnBasePath;
    private string _projectFilePath;

    public CsProjectFileDefinition(string projectFilePath, string slnBasePath)
    {
      _slnBasePath = slnBasePath;
      _projectFilePath = projectFilePath;
      RelativeFilePath = Path.GetRelativePath(slnBasePath, projectFilePath);
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

    public CsProjectFileDefinition Deserialize(IReadOnlyDictionary<string, string> projectLabels, IReadOnlyDictionary<string, string> importLabels, XDocument document)
    {
      Type = GetProjectType(document);

      foreach(var reference in document.Descendants("PackageReference"))
      {
        var name = reference.Attribute("Include").Value;

        if(importLabels.ContainsKey(name))
        {
          ProjectReference.Add(importLabels[name]);
        }
        else if(name.StartsWith("Afas.Generator", StringComparison.OrdinalIgnoreCase) || projectLabels.ContainsKey(name))
        {
          ProjectReference.Add(projectLabels[name]);
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
        var name = Path.GetFileNameWithoutExtension(include);

        ProjectReference.Add(projectLabels[name]);
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