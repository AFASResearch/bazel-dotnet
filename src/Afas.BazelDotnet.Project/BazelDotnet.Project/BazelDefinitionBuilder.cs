using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Afas.BazelDotnet.Project
{
  internal class BazelDefinitionBuilder
  {
    private readonly CsProjectFileDefinition _definition;
    private readonly string _workspace;
    private readonly string _nugetWorkspace;
    private string _visibility = BazelDefinition.DefaultVisibility;

    public BazelDefinitionBuilder(CsProjectFileDefinition definition, string workspace, string nugetWorkspace)
    {
      _definition = definition;
      _workspace = workspace;
      _nugetWorkspace = nugetWorkspace;
    }

    public BazelDefinition Build()
    {
      return new BazelDefinition(
        _definition,
        Path.GetFileNameWithoutExtension(_definition.RelativeFilePath),
        GetRuleType(_definition),
        $"{Path.GetFileNameWithoutExtension(_definition.RelativeFilePath)}.dll",
        BuildSrcPatterns(_definition).ToArray(),
        GetDependencies().ToArray(),
        GetResources(),
        GetResx(),
        _definition.CopyToOutput,
        _definition.BazelData,
        _visibility,
        _definition.TestOnly
      );
    }

    private IEnumerable<string> GetDependencies() =>
      BuildExternalDependencies().Concat(BuildInternalDependencies());

    private (IReadOnlyCollection<string>, IReadOnlyCollection<string>)? GetResources()
    {
      var includes = _definition.EmbeddedResources.Where(e => e.Type == EmbeddedResourceType.Include).Select(e => e.GetNormalizedValue()).ToArray();
      var excludes = _definition.EmbeddedResources.Where(e => e.Type == EmbeddedResourceType.Remove).Select(e => e.GetNormalizedValue()).ToArray();

      if(!includes.Any())
      {
        return null;
      }

      return (includes, excludes);
    }

    private IReadOnlyCollection<string> GetResx()
    {
      return _definition.EmbeddedResources.Where(e => e.Type == EmbeddedResourceType.Update && e.Value.EndsWith(".resx")).Select(e => e.GetNormalizedValue()).ToArray();
    }

    private IEnumerable<string> BuildInternalDependencies()
    {
      foreach(var projectReference in _definition.ProjectReference.Distinct(StringComparer.OrdinalIgnoreCase))
      {
        yield return $"{projectReference}";
      }
    }

    private IEnumerable<string> BuildSrcPatterns(CsProjectFileDefinition definition)
    {
      var importProjects = definition.GetProperties("Import")
        .Select(x => x.Attribute("Project"))
        .Where(x => x is not null)
        .Select(x => $"[\"{BuildLabel(x.Value, definition.RelativeFilePath)}\"]")
        .ToList();

      var compileRemoves = definition.GetProperties("Compile")
        .Select(x => x.Attribute("Remove"))
        .Where(x => x is not null)
        .Select(x => $"\"{x.Value.Replace("\\", "/")}\"")
        .ToList();

      var excludePatterns = new List<string> { "\"**/obj/**\"", "\"**/bin/**\"" };
      excludePatterns.AddRange(compileRemoves);

      var excludeList = string.Join(", ", excludePatterns);
      var globPattern = $@"glob([""**/*.cs""], exclude = [{excludeList}])";

      if(importProjects.Any())
      {
        string importProjectList = string.Join(" + ", importProjects);

        yield return $@"{globPattern} + {importProjectList}";
        yield break;
      }

      yield return globPattern;
    }

    private string BuildLabel(string projectPath, string definitionPath)
    {
      var fullPath = Path.Combine(Path.GetDirectoryName(definitionPath), Path.GetDirectoryName(projectPath));
      string relativePath = Path.GetRelativePath(_workspace, fullPath).Replace('\\', '/');
      return $"//{relativePath}";
    }

    private IEnumerable<string> BuildExternalDependencies()
    {
      foreach(var reference in _definition.PackageReferences)
      {
        yield return $"@{_nugetWorkspace}//{reference.ToLower()}";
      }
    }

    private string GetLabel(string relativeFilePath)
    {
      return relativeFilePath
        // remove file name
        .Replace(Path.GetFileName(relativeFilePath), string.Empty, StringComparison.OrdinalIgnoreCase)
        // Fix slashes
        .Replace('\\', '/')
        .TrimEnd('/');
    }

    private string GetRuleType(CsProjectFileDefinition definition)
    {
      switch(definition.Type)
      {
        case ProjectType.Binary:
          return "core_binary";
        case ProjectType.Test:
          return "core_nunit3_test";
        case ProjectType.Library:
          return "core_library";
        default:
          throw new NotSupportedException($"{definition.Type} is not a supported project type");
      }
    }

    public BazelDefinitionBuilder Visibility(string target)
    {
      _visibility = target;
      return this;
    }
  }
}