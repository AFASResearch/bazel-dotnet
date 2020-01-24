using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Afas.BazelDotnet.Project
{
  internal class BazelDefinition
  {
    public BazelDefinition(string label, string type, string outputAssembly,
      IReadOnlyCollection<string> srcPatterns, IReadOnlyCollection<string> deps,
      (IReadOnlyCollection<string>, IReadOnlyCollection<string>)? resources,
      IReadOnlyCollection<string> resx,
      List<string> data)
    {
      Label = label;
      Type = type;
      OutputAssembly = outputAssembly;
      SrcPatterns = srcPatterns;
      Deps = deps;
      Resources = resources;
      Resx = resx;
      Data = data;
    }

    public string Label { get; }

    public string Type { get; }

    public string OutputAssembly { get; }

    public IReadOnlyCollection<string> SrcPatterns { get; }

    public IReadOnlyCollection<string> Deps { get; }

    public (IReadOnlyCollection<string> includes, IReadOnlyCollection<string> excludes)? Resources { get; }

    public IReadOnlyCollection<string> Resx { get; }

    public List<string> Data { get; }

    public string Serialize()
    {
      // load
      // package
      // rules
      return WriteRule();
    }

    private IEnumerable<string> GetUsedMethods()
    {
      if(!Type.Equals("core_nunit3_test"))
      {
        yield return Type;
      }

      if(Resources != null)
      {
        yield return "core_resource_multi";
      }

      if(Resx.Any())
      {
        yield return "core_resx";
      }
    }

    private string RenderResources()
    {
      var result = string.Empty;

      if(Resources != null)
      {
        var includes = string.Join(", ", Resources.Value.includes.Select(Quote));
        var excludes = string.Join(", ", Resources.Value.excludes
          .Append("**/obj/**")
          .Append("**/bin/**")
          .Select(Quote));

        result += $"\ncore_resource_multi(name = \"Resources\", identifierBase = \"Afas\", srcs = glob([{includes}], exclude = [{excludes}]))\n" +
                  "resources.append(\"Resources\")\n";
      }

      if(Resx.Any())
      {
        result += string.Join("\n", Resx.Select(RenderResx)) + "\n";
      }

      return result;
      
      string RenderResx(string resx)
      {
        var name = resx.Replace("/", ".");
        return $@"core_resx(
    name = ""{name}"",
    src = ""{resx}"",
    out = ""Afas.{Path.GetFileNameWithoutExtension(name)}.resources"",
)
resources.append(""{name}"")";
      }
    }

    private string RenderLoad()
    {
      var result = string.Empty;

      var methods = string.Join(", ", GetUsedMethods().Select(Quote));

      if(methods.Any())
      {
        result += $"load(\"@io_bazel_rules_dotnet//dotnet:defs.bzl\", {methods})\n";
      }

      if(Type.Equals("core_nunit3_test"))
      {
        result += "load(\"//:test.bzl\", \"core_nunit3_test\")\n";
      }

      return result;
    }

    private string RenderData()
    {
      // TODO implement glob when needed
      var d = Data.Where(d => !d.Contains("*")).ToArray();

      if(!d.Any())
      {
        return string.Empty;
      }

      return $@"
  data = [
    {string.Join(",\n    ", d.Select(Quote))}
  ],";
    }

    private string WriteRule()
    {
      return
        $@"{RenderLoad()}resources = []
{RenderResources()}
{Type}(
  name = ""{Label}"",
  out = ""{OutputAssembly}"",
  srcs = {string.Join(",\n", SrcPatterns)},
  resources = resources,{RenderData()}
  deps = [
    #Adding built-in .Net libs
    {string.Join(",\n    ", Deps.Select(Quote))}
  ],
  dotnet_context_data = ""//:afas_context_data"",
  visibility = [""//visibility:public""]
)";
    }

    private static string Quote(string n) => $@"""{n}""";
  }
}