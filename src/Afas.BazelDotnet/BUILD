load("@io_bazel_rules_dotnet//dotnet:defs.bzl", "core_binary")
resources = []

core_binary(
  name = "Afas.BazelDotnet",
  out = "Afas.BazelDotnet.dll",
  srcs = glob(["**/*.cs"], exclude = ["**/obj/**", "**/bin/**"]),
  resources = resources,
  deps = [
    #Adding built-in .Net libs
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:netstandard.dll",
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:microsoft.csharp.dll",
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:system.reflection.dll",
    "@mcmaster.extensions.commandlineutils//:netcoreapp3.1_core",
    "//src/Afas.BazelDotnet.Nuget:Afas.BazelDotnet.Nuget",
    "//src/Afas.BazelDotnet.Project:Afas.BazelDotnet.Project"
  ],
  visibility = ["//visibility:public"]
)