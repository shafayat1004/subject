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

[<Fable.Core.JS.Pojo>]
type private UiActionJs
    ( kind: string, testId: string, label: string, route: string, component: string,
      detail: obj, timestamp: int64 ) =
    member val kind = kind
    member val testId = testId
    member val label = label
    member val route = route
    member val component = component
    member val detail = detail
    member val timestamp = timestamp

[<Fable.Core.JS.Pojo>]
type private InteractiveSnapshotJs
    ( testId: string, label: string, role: string, component: string, visible: bool,
      state: obj ) =
    member val testId = testId
    member val label = label
    member val role = role
    member val component = component
    member val visible = visible
    member val state = state

[<Fable.Core.JS.Pojo>]
type private UiSnapshotJs
    ( route: string, focused: obj, interactives: obj array, recentActions: obj array ) =
    member val route = route
    member val focused = focused
    member val interactives = interactives
    member val recentActions = recentActions

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

    let detailToJs (detail: Map<string, string>) : obj =
        detail
        |> Map.toList
        |> List.map (fun (k, v) -> (k, box v))
        |> createObj

    let actionToJs (a: UiAction) =
        UiActionJs(
            kindName a.Kind,
            a.TestId |> Option.defaultValue "",
            a.Label |> Option.defaultValue "",
            a.Route |> Option.defaultValue "",
            a.Component |> Option.defaultValue "",
            detailToJs a.Detail,
            a.Timestamp
        ) |> box

    let interactiveToJs (i: InteractiveSnapshot) =
        InteractiveSnapshotJs(
            i.TestId |> Option.defaultValue "",
            i.Label |> Option.defaultValue "",
            i.Role |> Option.defaultValue "",
            i.Component |> Option.defaultValue "",
            i.Visible,
            detailToJs i.State
        ) |> box

    let snapshotToJs () =
        UiSnapshotJs(
            currentRoute |> Option.defaultValue "",
            focused
            |> Option.map interactiveToJs
            |> Option.defaultValue JS.undefined,
            interactives
            |> Map.values
            |> Seq.filter (fun i -> i.Visible)
            |> Seq.map interactiveToJs
            |> Seq.toArray,
            actions
            |> List.take (min 50 (List.length actions))
            |> List.map actionToJs
            |> List.toArray
        ) |> box

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
