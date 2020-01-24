load("@io_bazel_rules_dotnet//dotnet:defs.bzl", "core_library")
resources = []

core_library(
  name = "Afas.BazelDotnet.Project",
  out = "Afas.BazelDotnet.Project.dll",
  srcs = glob(["**/*.cs"], exclude = ["**/obj/**", "**/bin/**"]),
  resources = resources,
  deps = [
    #Adding built-in .Net libs
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:netstandard.dll",
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:microsoft.csharp.dll",
    "@io_bazel_rules_dotnet//dotnet/stdlib.core:system.reflection.dll"
  ],
  visibility = ["//visibility:public"]
)