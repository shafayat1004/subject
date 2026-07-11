[<AutoOpen>]
module LibRouter.Components.EdgeSwipeBack

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open LibClient
open LibClient.JsInterop
open Rn
open Rn.Components
open Rn.Styles
open LibRouter.Components.Constructors

type LR with
    [<Component>]
    static member EdgeSwipeBack(
            goBack: unit -> unit,
            ?canGoBack: unit -> bool,
            ?edgeWidth: float,
            ?threshold: float,
            ?key: string
        ) : ReactElement =
        ignore key

        let canGoBack = canGoBack |> Option.defaultValue (fun () -> true)
        if not (canGoBack ()) then noElement else

        if Rn.Runtime.isWeb () then noElement else

        match Rn.Runtime.platform with
        | Native NativePlatform.IOS ->
            let edgeWidth = edgeWidth |> Option.defaultValue 24.0
            let threshold = threshold |> Option.defaultValue 12.0

            let initialPage = Hooks.useRef None
            let isActive = Hooks.useRef false
            let lastPageRef = Hooks.useRef (0.0, 0.0)

            let tryReadPage (e: obj) : (float * float) option =
                let ne = e?nativeEvent
                if isNullOrUndefined ne then
                    None
                else
                    Some (ne?pageX, ne?pageY)

            let pageFrom (e: obj) : float * float =
                match tryReadPage e with
                | Some coords ->
                    lastPageRef.current <- coords
                    coords
                | None ->
                    lastPageRef.current

            let captureStart (e: obj) : unit =
                let (pgX, pgY) = pageFrom e
                initialPage.current <- Some (pgX, pgY)

            let viewProps = createEmpty

            viewProps?onStartShouldSetResponder <- fun (_e: obj) -> false

            viewProps?onMoveShouldSetResponder <- fun (e: obj) ->
                match tryReadPage e with
                | Some (pgX, pgY) ->
                    match initialPage.current with
                    | None ->
                        initialPage.current <- Some (pgX, pgY)
                        false
                    | Some (initX, initY) ->
                        let dx = pgX - initX
                        let dy = pgY - initY
                        dx > threshold && abs dx > abs dy
                | None ->
                    false

            viewProps?onResponderGrant <- fun (e: obj) ->
                captureStart e
                isActive.current <- true

            viewProps?onResponderMove <- fun (e: obj) ->
                pageFrom e |> ignore

            viewProps?onResponderRelease <- fun (e: obj) ->
                let (curX, _) = pageFrom e
                match initialPage.current with
                | Some (initX, _) ->
                    if isActive.current && curX - initX > threshold then
                        goBack ()
                | None -> ()

                initialPage.current <- None
                isActive.current <- false

            viewProps?onResponderTerminate <- fun (_e: obj) ->
                initialPage.current <- None
                isActive.current <- false

            viewProps?onResponderTerminationRequest <- fun (_e: obj) -> true

            viewProps?importantForAccessibility <- "no"
            viewProps?accessibilityElementsHidden <- true
            viewProps?``aria-hidden`` <- true

            viewProps?style <- createObj [
                "position" ==> "absolute"
                "left" ==> 0
                "top" ==> 0
                "bottom" ==> 0
                "width" ==> edgeWidth
                "zIndex" ==> 9999
                "backgroundColor" ==> "transparent"
            ]

            Rn.RnPrimitives.createElement Rn.RnPrimitives.View viewProps [||]

        | _ ->
            noElement
