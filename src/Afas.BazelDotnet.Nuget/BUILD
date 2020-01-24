load("@io_bazel_rules_dotnet//dotnet:defs.bzl", "core_library")
resources = []

core_library(
  name = "Afas.BazelDotnet.Nuget",
  out = "Afas.BazelDotnet.Nuget.dll",
  srcs = glob(["**/*.cs"], exclude = ["**/obj/**", "**/bin/**"]),
  resources = resources,
  deps = [
    #Adding built-in .Net libs
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:netstandard.dll",
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:microsoft.csharp.dll",
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:system.reflection.dll",
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:system.xml.linq.dll",
    "@nuget.commands//:netcoreapp3.1_core",
    "@nuget.common//:netcoreapp3.1_core",
    "@nuget.configuration//:netcoreapp3.1_core",
    "@nuget.dependencyresolver.core//:netcoreapp3.1_core",
    "@nuget.frameworks//:netcoreapp3.1_core",
    "@nuget.packagemanagement//:netcoreapp3.1_core",
    "@nuget.packaging.core//:netcoreapp3.1_core",
    "@nuget.packaging//:netcoreapp3.1_core",
    "@nuget.projectmodel//:netcoreapp3.1_core",
    "@nuget.protocol//:netcoreapp3.1_core",
    "@nuget.resolver//:netcoreapp3.1_core",
    "@nuget.versioning//:netcoreapp3.1_core"
  ],
  visibility = ["//visibility:public"]
)