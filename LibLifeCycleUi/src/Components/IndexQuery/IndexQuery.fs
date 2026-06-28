[<AutoOpen>]
module LibLifeCycleUi.Components.IndexQuery

open Fable.React
open LibClient
open LibClient.Components
open ReactXP.Components

type UiLifeCycle with
    [<Component>]
    static member IndexQuery (something: int, ?key: string) : ReactElement =
        ignore key

        RX.View (
            children =
                [|
                    LC.UiText $"Take a deep breath. Here's something: {something}."
                |]
        )
