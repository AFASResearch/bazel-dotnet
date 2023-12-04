using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Afas.BazelDotnet.Project
{
  internal class CsProjectFileDefinition
  {
    private XDocument _document;

    public CsProjectFileDefinition(string projectFilePath, string slnBasePath)
    {
      RelativeFilePath = Path.GetRelativePath(slnBasePath, projectFilePath);
      PackageReferences = new List<string>
      {
        "Microsoft.NETCore.App.Ref",
      };
      ProjectReference = new List<string>();
      BazelData = new List<string>();
      EmbeddedResources = new List<EmbeddedResourceDefinition>();
      CopyToOutput = new List<string>();
    }

    public string RelativeFilePath { get; }

    public ProjectType Type { get; private set; }

    public List<string> PackageReferences { get; }

    public List<string> ProjectReference { get; }

    public List<string> BazelData { get; }

    public List<EmbeddedResourceDefinition> EmbeddedResources { get; }

    public List<string> CopyToOutput { get; }

    public bool TestOnly { get; private set; }

    public string ReadPropertyValue(string name) => _document.Descendants(name).LastOrDefault()?.Value;

    public IEnumerable<XElement> GetProperties(string name) => _document.Descendants(name);

    public (string glob, string filegroups) ReadItems(string name, string visibility)
    {
      var includes = new List<string>();
      var imports = new List<string>();
      var exports = new List<string>();
      var excludes = new List<string>
      {
        "**/obj/**",
        "**/bin/**",
      };

      foreach(var item in _document.Descendants(name))
      {
        var include = item.Attribute("Include")?.Value ?? item.Attribute("Update")?.Value;
        var remove = item.Attribute("Remove")?.Value;
        var filegroupName = item.Attribute("Name")?.Value;

        if(include != null)
        {
          if(include.StartsWith("**"))
          {
            if(string.IsNullOrEmpty(filegroupName))
            {
              filegroupName = include.Substring(3, include.IndexOfAny(new[] { '\\', '/' }, 3) - 3);
            }
            exports.Add($@"filegroup(
  name = {Quote(filegroupName)},
  srcs = glob([{Quote(include)}], exclude = [""**/obj/**"", ""**/bin/**""]),
  visibility = [{Quote(visibility)}],
)");
            imports.Add(filegroupName);
          }
          else if(include.StartsWith(".."))
          {
            var folder = Path.GetDirectoryName(RelativeFilePath);
            while(include.StartsWith(".."))
            {
              include = include.Substring(3);
              folder = Path.GetDirectoryName(folder);
            }

            imports.Add($"//{Path.Combine(folder, include).Replace("\\", "/").Replace("/**/", ":").Replace("/*.json", "")}");
          }
          else
          {
            includes.Add(include);
          }
        }

        if(remove != null)
        {
          excludes.Add(remove);
        }
      }

      var sb = new StringBuilder();

      if(includes.Count > 0)
      {
        sb.Append($"glob([{string.Join(", ", includes.Select(Quote))}], exclude = [{string.Join(", ", excludes.Select(Quote))}])");
      }

      if(imports.Count > 0)
      {
        if(sb.Length > 0)
        {
          sb.Append(" + ");
        }
        sb.Append(@$"[
    {string.Join(",\n    ", imports.Select(Quote))}
  ]");
      }

      return (sb.Length > 0 ? sb.ToString() : null, string.Join("\n", exports));
    }

    public (string, string) ReadTargets(string visibility)
    {
      var filegroups = new List<string>();
      var coreLibaries = new List<string>();

      var targets = _document.Descendants("Target");

      foreach(var target in targets)
      {
        switch(target.Attribute("Name")?.Value)
        {
          case "GenereateDefinitionsNupkg":
            var fileName = Path.GetFileNameWithoutExtension(
              GetNuspecFileValue(target.Attribute("Name")?.Value));

            var globPatterns = new List<string>
            {
              "**/Definitions/*.json", "**/Definitions/*/*.json",
              "**/Typed/*.json", "**/Typed/*/*.json",
            };
            var globPatternsString = string.Join(", ", globPatterns.Select(Quote));

            filegroups.Add($@"filegroup(
  name = {Quote(fileName + "__data")},
  srcs = glob([{globPatternsString}]),
  visibility = [{Quote(visibility)}]
)");
            coreLibaries.Add($@"core_library(
  name = {Quote(fileName)},
  out = {Quote(fileName + ".dll")},
  data = [{Quote(fileName + "__data")}],
  # currently this produces an assembly.
  deps = [{Quote("@nuget//microsoft.netcore.app.ref")}],
  dotnet_context_data = {Quote("//:afas_context_data")},
  visibility = [{Quote(visibility)}]
)");
            break;
          default:
            continue;
        }
      }

      return (filegroups.Any() ? string.Join("\n", filegroups) + "\n" : null,
          coreLibaries.Any() ? string.Join("\n", coreLibaries) + "\n" : null);
    }

    private static string Quote(string n) => $@"""{n.Replace('\\', '/')}""";

    private string GetNuspecFileValue(string targetName)
    {
      var target = _document.Descendants("Target")
        .FirstOrDefault(x => x.Attribute("Name")?.Value == targetName);

      var execElement = target?.Element("Exec");

      if(execElement is null)
      {
        return null;
      }

      var command = execElement.Attribute("Command")?.Value;

      if(string.IsNullOrEmpty(command))
      {
        return null;
      }

      var nuspecFileMatch = Regex.Match(command, @"-p:NuspecFile=\.(\\|\/)([^ ]+\.nuspec)");
      return nuspecFileMatch.Success ? nuspecFileMatch.Groups[2].Value : null;
    }

    public bool IsWebSdk => string.Equals(_document.Root?.Attribute("Sdk")?.Value, "Microsoft.NET.Sdk.Web");

    public CsProjectFileDefinition Deserialize(Func<string, string> csprojToLabel,
      IReadOnlyDictionary<string, string> importLabels, string projectFilePath)
    {
      _document = XDocument.Load(projectFilePath);
      var projectFileDir = Path.GetDirectoryName(projectFilePath);

      Type = GetProjectType(_document);

      foreach(var reference in _document.Descendants("PackageReference"))
      {
        var name = reference.Attribute("Include").Value;

        if(importLabels.ContainsKey(name))
        {
          ProjectReference.Add(importLabels[name]);
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

      foreach(var frameworkReference in _document.Descendants("FrameworkReference"))
      {
        // TODO naming .Ref?
        var name = frameworkReference.Attribute("Include").Value;
        PackageReferences.Add($"{name}.Ref");
      }

      foreach(var descendant in _document.Descendants("ProjectReference"))
      {
        var include = descendant.Attribute("Include").Value;
        var name = Path.GetFileNameWithoutExtension(include);

        if(importLabels.ContainsKey(name))
        {
          ProjectReference.Add(importLabels[name]);
        }
        else
        {
          ProjectReference.Add(csprojToLabel(Path.Combine(projectFileDir, include)));
        }
      }

      foreach(var bazelDataArray in _document.Descendants("BazelData"))
      {
        BazelData.AddRange(bazelDataArray.Value.Split(';'));
      }

      foreach(var resource in _document.Descendants("EmbeddedResource"))
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

      foreach(var copyNode in _document.Descendants("CopyToOutputDirectory"))
      {
        // PreserveNewest ?

        var include = copyNode.Parent?.Attribute("Include")?.Value
                   ?? copyNode.Parent?.Attribute("Update")?.Value;
        if(include != null)
        {
          CopyToOutput.Add(include.Replace("\\", "/"));
        }
      }

      TestOnly = string.Equals(_document.Descendants("BazelTestOnly").LastOrDefault()?.Value, "true", StringComparison.OrdinalIgnoreCase);

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