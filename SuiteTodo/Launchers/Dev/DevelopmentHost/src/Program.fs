module SuiteTodo.LifeCycleHost.Program

open LibLifeCycleHost.Host
open LibLifeCycleHost.Host.DevelopmentHost
open System.IO
open System.Reflection

let private ecosystem =
    SuiteTodo.LifeCycles.AllLifeCycles.todoEcosystem.Ecosystem

[<EntryPoint>]
let main args =
    Assembly.GetEntryAssembly().Location
    |> Path.GetDirectoryName
    |> Directory.SetCurrentDirectory

    let hostConfiguration = {
        ConfigureServices = ignore
        Configure         = ignore
    }

    startDevelopmentHost hostConfiguration ecosystem (typeof<LibLifeCycleHostBuild.Build>.Assembly) args
