/// Dev-only structured UI action log and interactive registry for automation/AI snapshots.
module LibClient.UiActionLog

open Fable.Core
open Fable.Core.JsInterop
open System

type UiActionKind =
| Press
| Navigate
| InputCommit
| SidebarOpen
| SidebarClose
| Focus
| Blur
| DialogOpen
| DialogClose
| AsyncStateChange
| Announce

type UiAction = {
    Kind: UiActionKind
    TestId: string option
    Label: string option
    Route: string option
    Component: string option
    Detail: Map<string, string>
    Timestamp: int64
}

type UiActionInput = {
    Kind: UiActionKind
    TestId: string option
    Label: string option
    ComponentName: string option
    Detail: Map<string, string>
}

type InteractiveSnapshot = {
    TestId: string option
    Label: string option
    Role: string option
    Component: string option
    Visible: bool
    State: Map<string, string>
}

type UiSnapshot = {
    Route: string option
    Focused: InteractiveSnapshot option
    Interactives: InteractiveSnapshot list
    RecentActions: UiAction list
}

[<RequireQualifiedAccess>]
module private Registry =
    let mutable enabled = false
    let mutable currentRoute: string option = None
    let mutable focused: InteractiveSnapshot option = None
    let mutable interactives: Map<string, InteractiveSnapshot> = Map.empty
    let mutable actions: UiAction list = []

    let maxActions = 500

    let kindName kind =
        match kind with
        | UiActionKind.Press -> "Press"
        | UiActionKind.Navigate -> "Navigate"
        | UiActionKind.InputCommit -> "InputCommit"
        | UiActionKind.SidebarOpen -> "SidebarOpen"
        | UiActionKind.SidebarClose -> "SidebarClose"
        | UiActionKind.Focus -> "Focus"
        | UiActionKind.Blur -> "Blur"
        | UiActionKind.DialogOpen -> "DialogOpen"
        | UiActionKind.DialogClose -> "DialogClose"
        | UiActionKind.AsyncStateChange -> "AsyncStateChange"
        | UiActionKind.Announce -> "Announce"

    let actionToJs (a: UiAction) =
        createObj [
            "kind" ==> kindName a.Kind
            "testId" ==> (a.TestId |> Option.defaultValue "")
            "label" ==> (a.Label |> Option.defaultValue "")
            "route" ==> (a.Route |> Option.defaultValue "")
            "component" ==> (a.Component |> Option.defaultValue "")
            "detail" ==> (a.Detail |> Map.toList |> List.map (fun (k, v) -> (k, box v)) |> createObj)
            "timestamp" ==> a.Timestamp
        ]

    let interactiveToJs (i: InteractiveSnapshot) =
        createObj [
            "testId" ==> (i.TestId |> Option.defaultValue "")
            "label" ==> (i.Label |> Option.defaultValue "")
            "role" ==> (i.Role |> Option.defaultValue "")
            "component" ==> (i.Component |> Option.defaultValue "")
            "visible" ==> i.Visible
            "state" ==> (i.State |> Map.toList |> List.map (fun (k, v) -> (k, box v)) |> createObj)
        ]

    let snapshotToJs () =
        createObj [
            "route" ==> (currentRoute |> Option.defaultValue "")
            "focused" ==>
                (focused
                 |> Option.map interactiveToJs
                 |> Option.defaultValue JS.undefined)
            "interactives" ==>
                (interactives
                 |> Map.values
                 |> Seq.filter (fun i -> i.Visible)
                 |> Seq.map interactiveToJs
                 |> Seq.toArray)
            "recentActions" ==>
                (actions
                 |> List.take (min 50 (List.length actions))
                 |> List.map actionToJs
                 |> List.toArray)
        ]

let private isDevEnabled () =
#if DEBUG
    true
#else
    Registry.enabled
#endif

let enable () =
    Registry.enabled <- true

let setCurrentRoute route =
    if not (isDevEnabled ()) then ()
    else
        Registry.currentRoute <- Some route
        Registry.actions <- {
            Kind = UiActionKind.Navigate
            TestId = None
            Label = None
            Route = Some route
            Component = None
            Detail = Map.empty
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        } :: Registry.actions |> List.truncate Registry.maxActions

let record (input: UiActionInput) =
    if not (isDevEnabled ()) then ()
    else
        Registry.actions <- {
            Kind = input.Kind
            TestId = input.TestId
            Label = input.Label
            Route = Registry.currentRoute
            Component = input.ComponentName
            Detail = input.Detail
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        } :: Registry.actions |> List.truncate Registry.maxActions

let registerInteractive
        (key: string)
        (testId: string option)
        (label: string option)
        (role: string option)
        (componentName: string option)
        (visible: bool)
        (state: Map<string, string>) =
    if not (isDevEnabled ()) then ()
    else
        Registry.interactives <-
            Registry.interactives
            |> Map.add key {
                TestId = testId
                Label = label
                Role = role
                Component = componentName
                Visible = visible
                State = state
            }

let unregisterInteractive (key: string) =
    if not (isDevEnabled ()) then ()
    else
        Registry.interactives <- Registry.interactives |> Map.remove key

let setFocused (snapshot: InteractiveSnapshot option) =
    if not (isDevEnabled ()) then ()
    else Registry.focused <- snapshot

let recentActions () = Registry.actions

let snapshot () : obj = Registry.snapshotToJs ()

let installGlobalHook (globalObj: obj) (appName: string) =
    enable ()
    let eggshell =
        match globalObj?( "eggshell") with
        | null -> createObj []
        | existing -> existing
    let app =
        match eggshell?(appName) with
        | null -> createObj []
        | existing -> existing
    app?uiLog <- (fun () -> Registry.actions |> List.map Registry.actionToJs |> List.toArray)
    app?uiSnapshot <- Registry.snapshotToJs
    eggshell?(appName) <- app
    globalObj?("eggshell") <- eggshell

module UiObservability =
    let announce (message: string) (politeness: LibClient.Accessibility.AccessibilityLiveRegion) =
        record {
            Kind = UiActionKind.Announce
            TestId = None
            Label = Some message
            ComponentName = None
            Detail = Map [ "politeness", string politeness ]
        }
