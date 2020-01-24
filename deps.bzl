load("@io_bazel_rules_dotnet//dotnet:defs.bzl", "nuget_package")

def deps():
    nuget_package(
        name = "mcmaster.extensions.commandlineutils",
        package = "mcmaster.extensions.commandlineutils",
        version = "2.5.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/McMaster.Extensions.CommandLineUtils.dll",
        },
        net_deps = {
            "net45": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net45_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net45_system.valuetuple.dll",
            ],
            "net451": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net451_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net451_system.valuetuple.dll",
            ],
            "net452": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net452_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net452_system.valuetuple.dll",
            ],
            "net46": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net46_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net46_system.valuetuple.dll",
            ],
            "net461": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.valuetuple.dll",
            ],
            "net462": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.valuetuple.dll",
            ],
            "net47": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.valuetuple.dll",
            ],
            "net471": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.valuetuple.dll",
            ],
            "net472": [
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net472_system.runtime.interopservices.runtimeinformation.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net472_system.valuetuple.dll",
            ],
        },
        mono_deps = [
            "@io_bazel_rules_dotnet//dotnet/stdlib:system.runtime.interopservices.runtimeinformation.dll",
            "@io_bazel_rules_dotnet//dotnet/stdlib:system.valuetuple.dll",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/McMaster.Extensions.CommandLineUtils.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.commands",
        package = "nuget.commands",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Commands.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.credentials//:netcoreapp2.0_core",
               "@nuget.projectmodel//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.credentials//:netcoreapp2.1_core",
               "@nuget.projectmodel//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.credentials//:netcoreapp2.2_core",
               "@nuget.projectmodel//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.credentials//:netcoreapp3.1_core",
               "@nuget.projectmodel//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.credentials//:net461_net",
               "@nuget.projectmodel//:net461_net",
            ],
            "net462": [
               "@nuget.credentials//:net462_net",
               "@nuget.projectmodel//:net462_net",
            ],
            "net47": [
               "@nuget.credentials//:net47_net",
               "@nuget.projectmodel//:net47_net",
            ],
            "net471": [
               "@nuget.credentials//:net471_net",
               "@nuget.projectmodel//:net471_net",
            ],
            "net472": [
               "@nuget.credentials//:net472_net",
               "@nuget.projectmodel//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.credentials//:netstandard2.0_net",
               "@nuget.projectmodel//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.credentials//:mono",
            "@nuget.projectmodel//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Commands.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.common",
        package = "nuget.common",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Common.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.frameworks//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.frameworks//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.frameworks//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.frameworks//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.frameworks//:net461_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.diagnostics.process.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.threading.thread.dll",
            ],
            "net462": [
               "@nuget.frameworks//:net462_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.diagnostics.process.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.threading.thread.dll",
            ],
            "net47": [
               "@nuget.frameworks//:net47_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.diagnostics.process.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.threading.thread.dll",
            ],
            "net471": [
               "@nuget.frameworks//:net471_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.diagnostics.process.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.threading.thread.dll",
            ],
            "net472": [
               "@nuget.frameworks//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.frameworks//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.frameworks//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Common.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.configuration",
        package = "nuget.configuration",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Configuration.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.common//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.common//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.common//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.common//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.common//:net461_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.security.cryptography.protecteddata.dll",
            ],
            "net462": [
               "@nuget.common//:net462_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.security.cryptography.protecteddata.dll",
            ],
            "net47": [
               "@nuget.common//:net47_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.security.cryptography.protecteddata.dll",
            ],
            "net471": [
               "@nuget.common//:net471_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.security.cryptography.protecteddata.dll",
            ],
            "net472": [
               "@nuget.common//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.common//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.common//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Configuration.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.dependencyresolver.core",
        package = "nuget.dependencyresolver.core",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.DependencyResolver.Core.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.librarymodel//:netcoreapp2.0_core",
               "@nuget.protocol//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.librarymodel//:netcoreapp2.1_core",
               "@nuget.protocol//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.librarymodel//:netcoreapp2.2_core",
               "@nuget.protocol//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.librarymodel//:netcoreapp3.1_core",
               "@nuget.protocol//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.librarymodel//:net461_net",
               "@nuget.protocol//:net461_net",
            ],
            "net462": [
               "@nuget.librarymodel//:net462_net",
               "@nuget.protocol//:net462_net",
            ],
            "net47": [
               "@nuget.librarymodel//:net47_net",
               "@nuget.protocol//:net47_net",
            ],
            "net471": [
               "@nuget.librarymodel//:net471_net",
               "@nuget.protocol//:net471_net",
            ],
            "net472": [
               "@nuget.librarymodel//:net472_net",
               "@nuget.protocol//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.librarymodel//:netstandard2.0_net",
               "@nuget.protocol//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.librarymodel//:mono",
            "@nuget.protocol//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.DependencyResolver.Core.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.frameworks",
        package = "nuget.frameworks",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Frameworks.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Frameworks.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.packagemanagement",
        package = "nuget.packagemanagement",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/net472/NuGet.PackageManagement.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/net472/NuGet.PackageManagement.dll",
            ],
        },
        net_deps = {
            "net472": [
               "@nuget.commands//:net472_net",
               "@nuget.resolver//:net472_net",
               "@microsoft.web.xdt//:net472_net",
            ],
        },
        mono_deps = [
            "@nuget.commands//:mono",
            "@nuget.resolver//:mono",
            "@microsoft.web.xdt//:mono",
        ],
    )
    nuget_package(
        name = "nuget.packaging.core",
        package = "nuget.packaging.core",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Packaging.Core.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.packaging//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.packaging//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.packaging//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.packaging//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.packaging//:net461_net",
            ],
            "net462": [
               "@nuget.packaging//:net462_net",
            ],
            "net47": [
               "@nuget.packaging//:net47_net",
            ],
            "net471": [
               "@nuget.packaging//:net471_net",
            ],
            "net472": [
               "@nuget.packaging//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.packaging//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.packaging//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Packaging.Core.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.packaging",
        package = "nuget.packaging",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Packaging.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.configuration//:netcoreapp2.0_core",
               "@nuget.versioning//:netcoreapp2.0_core",
               "@newtonsoft.json//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.configuration//:netcoreapp2.1_core",
               "@nuget.versioning//:netcoreapp2.1_core",
               "@newtonsoft.json//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.configuration//:netcoreapp2.2_core",
               "@nuget.versioning//:netcoreapp2.2_core",
               "@newtonsoft.json//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.configuration//:netcoreapp3.1_core",
               "@nuget.versioning//:netcoreapp3.1_core",
               "@newtonsoft.json//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.configuration//:net461_net",
               "@nuget.versioning//:net461_net",
               "@newtonsoft.json//:net461_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.dynamic.runtime.dll",
            ],
            "net462": [
               "@nuget.configuration//:net462_net",
               "@nuget.versioning//:net462_net",
               "@newtonsoft.json//:net462_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.dynamic.runtime.dll",
            ],
            "net47": [
               "@nuget.configuration//:net47_net",
               "@nuget.versioning//:net47_net",
               "@newtonsoft.json//:net47_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.dynamic.runtime.dll",
            ],
            "net471": [
               "@nuget.configuration//:net471_net",
               "@nuget.versioning//:net471_net",
               "@newtonsoft.json//:net471_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.dynamic.runtime.dll",
            ],
            "net472": [
               "@nuget.configuration//:net472_net",
               "@nuget.versioning//:net472_net",
               "@newtonsoft.json//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.configuration//:netstandard2.0_net",
               "@nuget.versioning//:netstandard2.0_net",
               "@newtonsoft.json//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.configuration//:mono",
            "@nuget.versioning//:mono",
            "@newtonsoft.json//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Packaging.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.projectmodel",
        package = "nuget.projectmodel",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.ProjectModel.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.dependencyresolver.core//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.dependencyresolver.core//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.dependencyresolver.core//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.dependencyresolver.core//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.dependencyresolver.core//:net461_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.dynamic.runtime.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.threading.thread.dll",
            ],
            "net462": [
               "@nuget.dependencyresolver.core//:net462_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.dynamic.runtime.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.threading.thread.dll",
            ],
            "net47": [
               "@nuget.dependencyresolver.core//:net47_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.dynamic.runtime.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.threading.thread.dll",
            ],
            "net471": [
               "@nuget.dependencyresolver.core//:net471_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.dynamic.runtime.dll",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.threading.thread.dll",
            ],
            "net472": [
               "@nuget.dependencyresolver.core//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.dependencyresolver.core//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.dependencyresolver.core//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.ProjectModel.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.protocol",
        package = "nuget.protocol",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Protocol.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.packaging//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.packaging//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.packaging//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.packaging//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.packaging//:net461_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.dynamic.runtime.dll",
            ],
            "net462": [
               "@nuget.packaging//:net462_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.dynamic.runtime.dll",
            ],
            "net47": [
               "@nuget.packaging//:net47_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.dynamic.runtime.dll",
            ],
            "net471": [
               "@nuget.packaging//:net471_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.dynamic.runtime.dll",
            ],
            "net472": [
               "@nuget.packaging//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.packaging//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.packaging//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Protocol.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.resolver",
        package = "nuget.resolver",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Resolver.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.protocol//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.protocol//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.protocol//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.protocol//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.protocol//:net461_net",
            ],
            "net462": [
               "@nuget.protocol//:net462_net",
            ],
            "net47": [
               "@nuget.protocol//:net47_net",
            ],
            "net471": [
               "@nuget.protocol//:net471_net",
            ],
            "net472": [
               "@nuget.protocol//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.protocol//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.protocol//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Resolver.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.versioning",
        package = "nuget.versioning",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Versioning.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Versioning.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.credentials",
        package = "nuget.credentials",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.Credentials.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.protocol//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.protocol//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.protocol//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.protocol//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.protocol//:net461_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net461_system.runtime.serialization.formatters.dll",
            ],
            "net462": [
               "@nuget.protocol//:net462_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net462_system.runtime.serialization.formatters.dll",
            ],
            "net47": [
               "@nuget.protocol//:net47_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net47_system.runtime.serialization.formatters.dll",
            ],
            "net471": [
               "@nuget.protocol//:net471_net",
               "@io_bazel_rules_dotnet//dotnet/stdlib.net:net471_system.runtime.serialization.formatters.dll",
            ],
            "net472": [
               "@nuget.protocol//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.protocol//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.protocol//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.Credentials.dll",
            ],
        },
    )
    nuget_package(
        name = "nuget.librarymodel",
        package = "nuget.librarymodel",
        version = "5.4.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard2.0/NuGet.LibraryModel.dll",
        },
        core_deps = {
            "netcoreapp2.0": [
               "@nuget.common//:netcoreapp2.0_core",
               "@nuget.versioning//:netcoreapp2.0_core",
            ],
            "netcoreapp2.1": [
               "@nuget.common//:netcoreapp2.1_core",
               "@nuget.versioning//:netcoreapp2.1_core",
            ],
            "netcoreapp2.2": [
               "@nuget.common//:netcoreapp2.2_core",
               "@nuget.versioning//:netcoreapp2.2_core",
            ],
            "netcoreapp3.1": [
               "@nuget.common//:netcoreapp3.1_core",
               "@nuget.versioning//:netcoreapp3.1_core",
            ],
        },
        net_deps = {
            "net461": [
               "@nuget.common//:net461_net",
               "@nuget.versioning//:net461_net",
            ],
            "net462": [
               "@nuget.common//:net462_net",
               "@nuget.versioning//:net462_net",
            ],
            "net47": [
               "@nuget.common//:net47_net",
               "@nuget.versioning//:net47_net",
            ],
            "net471": [
               "@nuget.common//:net471_net",
               "@nuget.versioning//:net471_net",
            ],
            "net472": [
               "@nuget.common//:net472_net",
               "@nuget.versioning//:net472_net",
            ],
            "netstandard2.0": [
               "@nuget.common//:netstandard2.0_net",
               "@nuget.versioning//:netstandard2.0_net",
            ],
        },
        mono_deps = [
            "@nuget.common//:mono",
            "@nuget.versioning//:mono",
        ],
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard2.0/NuGet.LibraryModel.dll",
            ],
        },
    )
    nuget_package(
        name = "newtonsoft.json",
        package = "newtonsoft.json",
        version = "9.0.1",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.0/Newtonsoft.Json.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.0/Newtonsoft.Json.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.native.system",
        package = "runtime.native.system",
        version = "4.3.0",
    )
    nuget_package(
        name = "runtime.win.microsoft.win32.primitives",
        package = "runtime.win.microsoft.win32.primitives",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "runtimes/win/lib/netstandard1.3/Microsoft.Win32.Primitives.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "runtimes/win/lib/netstandard1.3/Microsoft.Win32.Primitives.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.collections",
        package = "runtime.any.system.collections",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Collections.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Collections.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.win.system.diagnostics.debug",
        package = "runtime.win.system.diagnostics.debug",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "runtimes/win/lib/netstandard1.3/System.Diagnostics.Debug.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "runtimes/win/lib/netstandard1.3/System.Diagnostics.Debug.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.globalization",
        package = "runtime.any.system.globalization",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Globalization.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Globalization.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.io",
        package = "runtime.any.system.io",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.5/System.IO.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.5/System.IO.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.win.system.io.filesystem",
        package = "runtime.win.system.io.filesystem",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "runtimes/win/lib/netstandard1.3/System.IO.FileSystem.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "runtimes/win/lib/netstandard1.3/System.IO.FileSystem.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.resources.resourcemanager",
        package = "runtime.any.system.resources.resourcemanager",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Resources.ResourceManager.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Resources.ResourceManager.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.runtime",
        package = "runtime.any.system.runtime",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.5/System.Runtime.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.5/System.Runtime.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.win.system.runtime.extensions",
        package = "runtime.win.system.runtime.extensions",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "runtimes/win/lib/netstandard1.5/System.Runtime.Extensions.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "runtimes/win/lib/netstandard1.5/System.Runtime.Extensions.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.runtime.handles",
        package = "runtime.any.system.runtime.handles",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Runtime.Handles.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Runtime.Handles.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.runtime.interopservices",
        package = "runtime.any.system.runtime.interopservices",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.6/System.Runtime.InteropServices.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.6/System.Runtime.InteropServices.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.text.encoding",
        package = "runtime.any.system.text.encoding",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Text.Encoding.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Text.Encoding.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.text.encoding.extensions",
        package = "runtime.any.system.text.encoding.extensions",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Text.Encoding.Extensions.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Text.Encoding.Extensions.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.threading.tasks",
        package = "runtime.any.system.threading.tasks",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Threading.Tasks.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Threading.Tasks.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.reflection",
        package = "runtime.any.system.reflection",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.5/System.Reflection.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.5/System.Reflection.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.reflection.primitives",
        package = "runtime.any.system.reflection.primitives",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Reflection.Primitives.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Reflection.Primitives.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.diagnostics.tools",
        package = "runtime.any.system.diagnostics.tools",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Diagnostics.Tools.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Diagnostics.Tools.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.reflection.extensions",
        package = "runtime.any.system.reflection.extensions",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.3/System.Reflection.Extensions.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.3/System.Reflection.Extensions.dll",
            ],
        },
    )
    nuget_package(
        name = "runtime.any.system.diagnostics.tracing",
        package = "runtime.any.system.diagnostics.tracing",
        version = "4.3.0",
        core_lib = {
            "netcoreapp3.1": "lib/netstandard1.5/System.Diagnostics.Tracing.dll",
        },
        core_files = {
            "netcoreapp3.1": [
               "lib/netstandard1.5/System.Diagnostics.Tracing.dll",
            ],
        },
    )
