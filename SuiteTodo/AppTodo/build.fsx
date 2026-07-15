// build.fsx script cobbles all referenced scripts together to avoid
// errors caused by loading same modules multiple times, and sets Fake execution context

#load "../../Fake/PackageReferences.fsx"

open System
open System.IO
open Fake.Core

let execContext =
    let scriptPath = Path.Combine (__SOURCE_DIRECTORY__, "./build-AppTodo.fsx")
    let scriptArgs = (fsi.CommandLineArgs |> List.ofSeq |> List.skip 1)
    Context.FakeExecutionContext.Create false scriptPath scriptArgs

Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

#load "./build-AppTodo.fsx"
