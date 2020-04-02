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
    private readonly string _nugetWorkspace;

    public BazelDefinitionBuilder(CsProjectFileDefinition definition, string nugetWorkspace)
    {
      _definition = definition;
      _nugetWorkspace = nugetWorkspace;
    }

    public BazelDefinition Build()
    {
      return new BazelDefinition(
        Path.GetFileNameWithoutExtension(_definition.RelativeFilePath),
        GetRuleType(_definition),
        $"{Path.GetFileNameWithoutExtension(_definition.RelativeFilePath)}.dll",
        BuildSrcPatterns().ToArray(),
        GetDependencies().ToArray(),
        GetAnalyzers().ToArray(),
        GetResources(),
        GetResx(),
        _definition.CopyToOutput
      );
    }

    private IEnumerable<string> GetDependencies() =>
      GetAssemblyDependencies().Concat(BuildExternalDependencies()).Concat(BuildInternalDependencies());

    private IEnumerable<string> GetAnalyzers()
    {
      foreach(var reference in _definition.Analyzers)
      {
        if(string.IsNullOrEmpty(_nugetWorkspace))
        {
          yield return $"@{reference.ToLower()}//:netcoreapp3.1_core";
        }
        else
        {
          yield return $"@{_nugetWorkspace}//{reference.ToLower()}:netcoreapp3.1_core";
        }
      }
    }

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

    private IEnumerable<string> GetAssemblyDependencies()
    {
      yield return "@io_bazel_rules_dotnet//dotnet/stdlib.core:netstandard.dll";
      yield return "@io_bazel_rules_dotnet//dotnet/stdlib.core:microsoft.csharp.dll";
      // shouldnt this be part of netstandard?
      yield return "@io_bazel_rules_dotnet//dotnet/stdlib.core:system.reflection.dll";
    }

    private IEnumerable<string> BuildSrcPatterns()
    {
      // { GetLabel(_definition.RelativeFilePath)}/
      yield return $@"glob([""**/*.cs""], exclude = [""**/obj/**"", ""**/bin/**""])";
    }

    private IEnumerable<string> BuildExternalDependencies()
    {
      foreach(var reference in _definition.PackageReferences)
      {
        if(string.IsNullOrEmpty(_nugetWorkspace))
        {
          yield return $"@{reference.ToLower()}//:netcoreapp3.1_core";
        }
        else
        {
          yield return $"@{_nugetWorkspace}//{reference.ToLower()}:netcoreapp3.1_core";
        }
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
  }
}