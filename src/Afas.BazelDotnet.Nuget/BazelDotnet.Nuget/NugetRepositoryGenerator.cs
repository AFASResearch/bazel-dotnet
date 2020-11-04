using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;

namespace Afas.BazelDotnet.Nuget
{
  public class NugetRepositoryGenerator
  {
    private readonly string _nugetConfig;
    private readonly ILookup<string, (string target, string configSetting)> _imports;

    public NugetRepositoryGenerator(string nugetConfig, ILookup<string, (string target, string configSetting)> imports)
    {
      _nugetConfig = nugetConfig;
      _imports = imports;
    }

    private async Task<NugetRepositoryEntry[]> ResolveLocalPackages(string targetFramework, string targetRuntime,
      IEnumerable<(string package, string version)> packageReferences)
    {
      ILogger logger = new ConsoleLogger();
      var settings = Settings.LoadSpecificSettings(Path.GetDirectoryName(_nugetConfig), Path.GetFileName(_nugetConfig));

      // ~/.nuget/packages

      using(var cache = new SourceCacheContext())
      {
        PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders = true;

        HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(() => new CredentialService(
          new AsyncLazy<IEnumerable<ICredentialProvider>>(() => GetCredentialProvidersAsync(settings)),
          nonInteractive: true,
          handlesDefaultCredentials: PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders));

        var dependencyGraphResolver = new TransitiveDependencyResolver(settings, logger, cache);

        foreach((string package, string version) v in packageReferences)
        {
          dependencyGraphResolver.AddPackageReference(v.package, v.version);
        }

        var dependencyGraph = await dependencyGraphResolver.ResolveGraph(targetFramework, targetRuntime).ConfigureAwait(false);
        var localPackages = await dependencyGraphResolver.DownloadPackages(dependencyGraph).ConfigureAwait(false);

        var entryBuilder = new NugetRepositoryEntryBuilder(dependencyGraph.Conventions)
          .WithTarget(new FrameworkRuntimePair(NuGetFramework.Parse(targetFramework), targetRuntime));

        var entries = localPackages.Select(entryBuilder.ResolveGroups).ToArray();

        var (frameworkEntries, frameworkOverrides) = await new FrameworkDependencyResolver(dependencyGraphResolver)
          .ResolveFrameworkPackages(entries, targetFramework)
          .ConfigureAwait(false);

        var overridenEntries = entries.Select(p =>
          frameworkOverrides.TryGetValue(p.LocalPackageSourceInfo.Package.Id, out var frameworkOverride)
            ? entryBuilder.BuildFrameworkOverride(p, frameworkOverride)
            : p);

        return frameworkEntries.Concat(overridenEntries).ToArray();
      }
    }

    private async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync(ISettings settings)
    {
      var providers = new List<ICredentialProvider>();

      var securePluginProviders = await (new SecurePluginCredentialProviderBuilder(PluginManager.Instance, canShowDialog: false, logger: NullLogger.Instance)).BuildAllAsync();
      providers.AddRange(securePluginProviders);

      IList<ICredentialProvider> pluginProviders = new List<ICredentialProvider>();
      var extensionLocator = new ExtensionLocator();
      pluginProviders = new PluginCredentialProviderBuilder(extensionLocator, settings, NullLogger.Instance)
        .BuildAll("Detailed") //Verbosity.Warning.ToString())
        .ToList();
      providers.AddRange(pluginProviders);
      if(pluginProviders.Any() || securePluginProviders.Any())
      {
        if(PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders)
        {
          providers.Add(new DefaultNetworkCredentialsCredentialProvider());
        }
      }

      return providers;
    }

    public async Task WriteRepository(string targetFramework, string targetRuntime, IEnumerable<(string package, string version)> packageReferences)
    {
      var packages = await ResolveLocalPackages(targetFramework, targetRuntime, packageReferences).ConfigureAwait(false);

      var symlinks = new HashSet<(string link, string target)>();

      foreach(var entryGroup in packages.GroupBy(e => e.LocalPackageSourceInfo.Package.Id, StringComparer.OrdinalIgnoreCase))
      {
        var id = entryGroup.Key.ToLower();
        bool isSingle = entryGroup.Count() == 1;

        if(isSingle)
        {
          WriteBuildFile(entryGroup.Single(), id);
        }
        else
        {
          WriteBuildFile(entryGroup, id);
        }

        // Possibly link multiple versions
        foreach(var entry in entryGroup)
        {
          symlinks.Add(($"{id}/{entry.LocalPackageSourceInfo.Package.Version}", entry.LocalPackageSourceInfo.Package.ExpandedPath));

          if(isSingle)
          {
            symlinks.Add(($"{id}/current", entry.LocalPackageSourceInfo.Package.ExpandedPath));
          }
        }
      }

      File.WriteAllText("symlinks_manifest", string.Join("\n", symlinks
        .Select(sl => $@"{sl.link} {sl.target}")));

      File.WriteAllText("link.cmd", @"
for /F ""usebackq tokens=1,2 delims= "" %%i in (""symlinks_manifest"") do mklink /J ""%%i"" ""%%j""
exit /b %errorlevel%
");
      var proc = Process.Start(new ProcessStartInfo("cmd.exe", "/C link.cmd"));
      proc.WaitForExit();

      if(proc.ExitCode != 0)
      {
        throw new Exception("Creating symlinks exited non 0");
      }
    }

    private void WriteBuildFile(NugetRepositoryEntry entry, string id)
    {
      var content = $@"package(default_visibility = [""//visibility:public""])
load(""@io_bazel_rules_dotnet//dotnet:defs.bzl"", ""core_import_library"")

exports_files([""contentfiles.txt""])

{CreateTarget(entry, isSingle: true)}";

      var filePath = $"{id}/BUILD";
      new FileInfo(filePath).Directory.Create();
      File.WriteAllText(filePath, content);

      // Also write a special file that lists the content files in this package.
      // This is to work around the fact that we cannot easily expose folders.
      File.WriteAllLines($"{id}/contentfiles.txt", GetContentFiles(entry).Select(v => $"current/{v}"));
    }

    private void WriteBuildFile(IGrouping<string, NugetRepositoryEntry> entryGroup, string id)
    {
      var content = $@"package(default_visibility = [""//visibility:public""])
load(""@io_bazel_rules_dotnet//dotnet:defs.bzl"", ""core_import_library"")

{string.Join("\n\n", entryGroup.Select(e => CreateTarget(e, isSingle: false)))}";

      var filePath = $"{id}/BUILD";
      new FileInfo(filePath).Directory.Create();
      File.WriteAllText(filePath, content);
    }

    private IEnumerable<string> GetContentFiles(NugetRepositoryEntry package)
    {
      var group = package.ContentFileGroups.SingleOrDefault();
      if(group?.Items.Any() == true)
      {
        // Symlink optimization to link the entire group folder e.g. contentFiles/any/netcoreapp3.1
        // We assume all files in a group have the same prefix
        //yield return string.Join('/', group.Items.First().Split('/').Take(3));

        foreach(var item in group.Items)
        {
          yield return item;
        }
      }
    }

    private string CreateTarget(NugetRepositoryEntry package, bool isSingle)
    {
      var identity = package.LocalPackageSourceInfo.Package;
      var folder = isSingle ? "current" : identity.Version.ToString();
      var libs = Array(package.RuntimeItemGroups.SingleOrDefault()?.Items.Select(v => $"{folder}/{v}"));
      var refs = Array(package.RefItemGroups.SingleOrDefault()?.Items.Select(v => v.StartsWith("//") ? v : $"{folder}/{v}"));
      var contentFiles = Array(GetContentFiles(package).Select(v => $"{folder}/{v}"));
      var analyzers = Array(package.AnalyzerItemGroups.SingleOrDefault()?.Items.Select(v => v.StartsWith("//") ? v : $"{folder}/{v}"));

      var deps = Array(package.DependencyGroups.SingleOrDefault()?.Packages
        .Select(p => $"//{p.Id.ToLower()}"));

      var name = identity.Id.ToLower();
      var alias = string.Empty;
      if(_imports.Contains(name))
      {
        var selects = string.Join("\n", _imports[name].Select(i => $@"    ""{i.configSetting}"": ""{i.target}"","));
        name += "__nuget";
        alias = $@"
alias(
  name = ""{identity.Id.ToLower()}"",
  actual = select({{
{selects}
    ""//conditions:default"": ""{name}"",
  }})
)
";
      }

      return $@"exports_files(glob([""{folder}/**"", ""{identity.Version}/**""]))

filegroup(
    name = ""content_files"",
    srcs = [{contentFiles}],
)
{alias}
core_import_library(
  name = ""{name}"",
  libs = [{libs}],
  refs = [{refs}],
  analyzers = [{analyzers}],
  deps = [{deps}],
  data = ["":content_files""],
  version = ""{identity.Version}"",
)";
    }

    private string Array(IEnumerable<string> elems)
    {
      if(elems?.Any() != true)
      {
        return string.Empty;
      }

      return $"\n{string.Join(",\n", elems.Select(e => $"    \"{e}\""))}\n  ";
    }
  }
}