[<AutoOpen>]
module LibLifeCycleUi.Components.IndexQuery

open Fable.React
open LibClient
open LibClient.Components
open Rn.Components

type UiLifeCycle with
    [<Component>]
    static member IndexQuery (something: int, ?key: string) : ReactElement =
        ignore key

        Rn.View (
            children =
                [|
                    LC.UiText $"Take a deep breath. Here's something: {something}."
                |]
        )
