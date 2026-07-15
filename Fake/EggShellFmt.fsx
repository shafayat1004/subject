#if !FAKE_BUILD_FSPROJ
#load "./PackageReferences.fsx"
#endif

namespace Egg.Shell.Fake

module EggShellFmt =

    open Fake.Core
    open Fake.IO

    let private executeEggShellFmt workingDirectory (maybeAdditionalParams: list<string>) =
        let args = "tool" :: "run" :: "eggshell-fmt" :: "--" :: maybeAdditionalParams

        CreateProcess.fromRawCommand "dotnet" args
        |> CreateProcess.ensureExitCode
        |> CreateProcess.withWorkingDirectory workingDirectory
        |> Proc.run

    let checkFormatting workingDirectory: unit =
        executeEggShellFmt workingDirectory [ "--check"; "." ]
        |> ignore
