// Genuinely-stateful component: a Mode state machine (Initial/InProgress/Error) driven by an async
// action. The class form used GetInitialEstate + SetEstate in Actions; the modern form uses
// `Hooks.useState` (see QuadStateful.fs, the 4-state sibling, as the reference template).
// Public types are nested under `module LC = module TriStateful = module Abstract` so [<AutoOpen>]
// doesn't leak the `Mode` type name (see LEARNINGS.md). External path: `LC.TriStateful.Abstract.Mode`.
[<AutoOpen>]
module LibClient.Components.TriStateful_Abstract

open Fable.React

open LibClient

module LC =
    module TriStateful =
        module Abstract =
            type RunAction = Async<Result<unit, string>> -> unit

            type [<RequireQualifiedAccess>] [<DefaultAugmentation(false)>] Mode =
            | Initial
            | InProgress
            | Error of string
            with
                member this.MaybeError : Option<string> =
                    match this with
                    | Error e -> Some e
                    | _       -> None

                member this.IsInProgress : bool =
                    match this with
                    | InProgress -> true
                    | _          -> false

            let toButtonState (mode: Mode) (runAction: Async<Result<unit, string>> -> unit) (makeTask: unit -> Async<Result<unit, string>>) : ButtonLowLevelState =
                match mode with
                | Mode.Initial ->
                    ButtonLowLevelState.Actionable
                        (fun _ ->
                            makeTask ()
                            |> runAction
                        )

                | Mode.InProgress ->
                    ButtonLowLevelState.InProgress

                | _ ->
                    ButtonLowLevelState.Disabled

open LC.TriStateful.Abstract

type LibClient.Components.Constructors.LC.TriStateful with
    [<Component>]
    static member Abstract(
            content: (Mode * (* runAction *) RunAction * (* reset *) (ReactEvent.Action -> unit)) -> ReactElement
        ) : ReactElement =
        let modeHook = Hooks.useState Mode.Initial

        let runAction (action: Async<Result<unit, string>>) : unit =
            modeHook.update Mode.InProgress
            async {
                let! actionResult = action
                let nextMode =
                    match actionResult with
                    | Ok _          -> Mode.Initial
                    | Error message -> Mode.Error message

                modeHook.update nextMode
            } |> startSafely

        let reset (_e: ReactEvent.Action) : unit =
            modeHook.update Mode.Initial

        content (modeHook.current, runAction, reset)
