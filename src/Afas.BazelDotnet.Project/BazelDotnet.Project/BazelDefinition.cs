using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Afas.BazelDotnet.Project
{
  internal class BazelDefinition
  {
    private readonly CsProjectFileDefinition _csProjectFileDefinition;
    internal const string DefaultVisibility = "//visibility:public";

    public BazelDefinition(CsProjectFileDefinition csProjectFileDefinition, string label, string type, string outputAssembly,
      IReadOnlyCollection<string> srcPatterns, IReadOnlyCollection<string> deps,
      (IReadOnlyCollection<string>, IReadOnlyCollection<string>)? resources,
      IReadOnlyCollection<string> resx,
      List<string> dataFiles,
      List<string> data,
      string visibility = DefaultVisibility,
      bool testOnly = false)
    {
      _csProjectFileDefinition = csProjectFileDefinition;
      Label = label;
      Type = type;
      OutputAssembly = outputAssembly;
      SrcPatterns = srcPatterns;
      Deps = deps;
      Resources = resources;
      Resx = resx;
      DataFiles = dataFiles;
      Data = data;
      Visibility = visibility;
      TestOnly = testOnly;
    }

    public string Label { get; }

    public string Type { get; }

    public string OutputAssembly { get; }

    public IReadOnlyCollection<string> SrcPatterns { get; }

    public IReadOnlyCollection<string> Deps { get; }

    public (IReadOnlyCollection<string> includes, IReadOnlyCollection<string> excludes)? Resources { get; }

    public IReadOnlyCollection<string> Resx { get; }

    public List<string> DataFiles { get; }

    public List<string> Data { get; }

    public string Visibility { get; }

    public bool TestOnly { get; }

    public string Serialize()
    {
      // load
      // package
      // rules
      return Regex.Replace(string.Join("", WriteRule()), "(?<!\r)\n", "\r\n");
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
        var output = $"Afas.{Path.GetFileNameWithoutExtension(name)}.resources";
        if(name.Equals(resx, StringComparison.OrdinalIgnoreCase))
        {
          name = $"_{name}";
        }
        return $@"core_resx(
    name = ""{name}"",
    src = ""{resx}"",
    out = ""{output}"",
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
        result += "load(\"//devtools/bazel:test.bzl\", \"core_nunit3_test\")\n";
      }

      return result;
    }

    private string RenderData()
    {
      if(!Data.Any() && !DataFiles.Any())
      {
        return "[]";
      }

      if(Data.Any() && DataFiles.Any())
      {
        return $@"[{string.Join(", ", Data.Select(Quote))}] + glob([{string.Join(", ", DataFiles.Select(Quote))}], exclude = [""**/obj/**"", ""**/bin/**""])";
      }

      if(Data.Any())
      {
        return $@"[{string.Join(", ", Data.Select(Quote))}]";
      }

      return $@"glob([{string.Join(", ", DataFiles.Select(Quote))}], exclude = [""**/obj/**"", ""**/bin/**""])";
    }

    private IEnumerable<string> WriteRule()
    {
      var srcs = _csProjectFileDefinition.ReadPropertyValue("BazelSrcs");
      var filegroups = _csProjectFileDefinition.ReadItems("FileGroup", "//visibility:public").Item2;
      var (additionalFiles, additionalFilegroups) = _csProjectFileDefinition.ReadItems("AdditionalFiles", Visibility);

      yield return @$"{RenderLoad()}resources = []{srcs}
{RenderResources()}

filegroup(
  name = ""{Label}__data"",
  srcs = {RenderData()},
  visibility = [""{Visibility}""]
)
{filegroups}{additionalFilegroups}
{Type}(
  name = ""{Label}"",
  out = ""{OutputAssembly}"",";

      if(_csProjectFileDefinition.IsWebSdk)
      {
        yield return @"
  runtime_properties = {
    ""System.GC.Server"": ""true""
  },";
      }

      if(TestOnly)
      {
        yield return @"
  testonly = True,";
      }

      if(string.Equals(_csProjectFileDefinition.ReadPropertyValue("Nullable"), "enable", StringComparison.OrdinalIgnoreCase))
      {
        yield return @"
  nullable = True,";
      }

      if(additionalFiles != null)
      {
        yield return $@"
  additional_files = {additionalFiles},";
      }

      yield return $@"
  srcs = {(string.IsNullOrEmpty(srcs) ? string.Join(",\n", SrcPatterns) : "srcs")},
  resources = resources,
  data = ["":{Label}__data""],
  deps = [
    {string.Join(",\n    ", Deps.Select(Quote))}
  ],
  dotnet_context_data = ""//:afas_context_data"",
  visibility = [""{Visibility}""]
)";
    }

    private static string Quote(string n) => $@"""{n}""";
  }
}