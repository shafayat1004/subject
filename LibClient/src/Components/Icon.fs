[<AutoOpen>]
module LibClient.Components.Icon

open Fable.Core.JsInterop
open Fable.React

open LibClient
open LibClient.Accessibility

open Rn.Components
open Rn.LegacyStyles
open Rn.Styles

[<RequireQualifiedAccess>]
module private Helpers =
    let extractColorAndSizeFromLegacyStyles (legacyStyles: Option<List<RuntimeStyles>>) : (Option<Color> * Option<int>) =
        match legacyStyles with
        | None -> (None, None)
        | Some styles ->
            let (colors, fontSizes) =
                styles
                |> List.fold
                    (fun (colors, fontSizes) currRuntimeStyles ->
                        match currRuntimeStyles with
                        | Rn.LegacyStyles.RuntimeStyles.StaticRules lazyRulesObject ->
                            lazyRulesObject.GetRawStyleRules
                            |> Seq.fold
                                (fun (colors, fontSizes) rawRule ->
                                    let (key, value) = rawRule :> obj :?> (string * obj)
                                    match key with
                                    | "color"    -> ((Color.InternalString (value :?> string)) :: colors, fontSizes)
                                    | "fontSize" -> (colors, value :?> int :: fontSizes)
                                    | _          -> (colors, fontSizes)
                                )
                                (colors, fontSizes)

                        | _ -> (colors, fontSizes)
                    )
                    ([], [])

            (List.tryHead colors, List.tryHead fontSizes)

    let extractColorAndSizeFromNewStyles (styles: Option<array<TextStyles>>) : (Option<Color> * Option<int>) =
        match styles with
        | None -> (None, None)
        | Some styles ->
            let (colors, fontSizes) =
                styles
                |> Array.fold
                    (fun (colors, fontSizes) currStyles ->
                        match (currStyles?color, currStyles?fontSize) with
                        | (Some color, Some size) -> ((Color.InternalString !!color) :: colors, !!size :: fontSizes)
                        | (Some color, None)      -> ((Color.InternalString !!color) :: colors, fontSizes)
                        | (None, Some size)       -> (colors, !!size :: fontSizes)
                        | _                       -> (colors, fontSizes)
                    )
                    ([], [])

            (List.tryHead colors, List.tryHead fontSizes)

    let extractColorAndSize (styles: Option<array<TextStyles>>) (legacyStyles: Option<List<RuntimeStyles>>) : Color * int =
        let (maybeColor, maybeFontSize) =
            match extractColorAndSizeFromLegacyStyles legacyStyles with
            | (None, None) -> extractColorAndSizeFromNewStyles styles
            | atLeastOneSome -> atLeastOneSome

        (maybeColor |> Option.getOrElse Color.DevRed, maybeFontSize |> Option.getOrElse 32)

type LibClient.Components.Constructors.LC with
    [<Component>]
    static member Icon(icon: LibClient.Icons.IconConstructor, ?styles: array<TextStyles>, ?decorative: bool, ?xLegacyStyles: List<RuntimeStyles>) : ReactElement =
#if DEBUG
        // snooze for a while
        if System.DateTime.Now > System.DateTime.Parse "2024-03-01" then
            let warningShownHook = Hooks.useRef false

            if not warningShownHook.current then
                warningShownHook.current <- true

                xLegacyStyles
                |> Option.iter (fun _ -> Browser.Dom.console.warn "LC.Icon is being used with legacy styles. Please update all usages to use styles rather than classes.")
#endif

        let decorative = defaultArg decorative false
        let color, size = (Helpers.extractColorAndSize styles xLegacyStyles)
        let iconElement = icon color size :> ReactElement

        if decorative then
            Rn.View(
                importantForAccessibility = LibClient.Accessibility.ImportantForAccessibility.No,
                children = [| iconElement |]
            )
        else
            iconElement