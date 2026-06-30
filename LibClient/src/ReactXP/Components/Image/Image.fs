[<AutoOpen>]
module ReactXP.Components.Image

open ReactXP.Helpers
open ReactXP.Types

open LibClient
open LibClient.JsInterop

open Fable.Core.JsInterop
open Fable.React
open Fable.Core
open Browser.Types

[<StringEnum>]
type AndroidResizeMethod =
| Auto
| Resize
| Scale

[<StringEnum>]
type ResizeMode =
| Stretch
| Contain
| Cover
| Auto
| Repeat

type Dimensions = {
    width:  int
    height: int
}

type Size =
| FromParentLayout of Option<LibClient.Output.Layout>
| FromStyles
| Raw
| ImplicitRaw

let ofUrl                 = LibClient.Services.ImageService.ImageSource.ofUrl
let ofPossiblyRelativeUrl = LibClient.Services.ImageService.ImageSource.ofPossiblyRelativeUrl

let mutable private implicitRawWarnedSources: Set<LibClient.Services.ImageService.ImageSource> = Set.empty

module private ImageRN =
    let unboxStyles (styles: array<ReactXP.Styles.FSharpDialect.ViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    // RN source must be {uri} or {uri, headers}; not a bare string
    let makeSource (uri: string) (headers: Headers option) : obj =
        match headers with
        | None    -> createObj ["uri" ==> uri]
        | Some h  -> createObj ["uri" ==> uri; "headers" ==> h]

    // RN doesn't have 'auto'; closest equivalent is 'center'
    let mapResizeMode (rm: ResizeMode option) : obj option =
        rm |> Option.map (function
            | Stretch -> box "stretch"
            | Contain -> box "contain"
            | Cover   -> box "cover"
            | Auto    -> box "center"
            | Repeat  -> box "repeat")

    // RN onLoad: {nativeEvent: {source: {width, height}}}
    let wrapOnLoad (f: (Dimensions -> unit) option) : obj option =
        f |> Option.map (fun handler ->
            box (fun (e: obj) ->
                let w = e?nativeEvent?source?width |> int
                let h = e?nativeEvent?source?height |> int
                handler { width = w; height = h }))

    let assignWebProps (props: obj) (title: string option) : unit =
        #if EGGSHELL_PLATFORM_IS_WEB
        // RNW uses 'alt' for accessible image description
        title |> Option.iter (fun v -> props?alt <- v)
        #endif
        ()

type ReactXP.Components.Constructors.RX with
    static member Image(
        source:               LibClient.Services.ImageService.ImageSource,
        ?size:                Size,
        ?headers:             Headers,
        ?accessibilityLabel:  string,
        ?resizeMode:          ResizeMode,
        ?androidResizeMethod: AndroidResizeMethod,
        ?title:               string,
        ?onLoad:              Dimensions -> unit,
        ?onError:             ErrorEvent -> unit,
        ?styles:              array<ReactXP.Styles.FSharpDialect.ViewStyles>,
        ?xLegacyStyles:       List<ReactXP.LegacyStyles.RuntimeStyles>
    ) =
        ignore xLegacyStyles

        // NOTE TODO cover/stretch optimized URL calculations not yet size-aware
        let maybeUpdatedSource =
            match defaultArg size Size.ImplicitRaw with
            | Raw -> source.Url |> Some
            | ImplicitRaw ->
                if not (implicitRawWarnedSources.Contains source) then
                    implicitRawWarnedSources <- implicitRawWarnedSources.Add source
                    Log.Warn ("An Image is used without Size being specified. This will render, but you should make an explicit choice. Source: {source}", source)
                source.Url |> Some
            | FromStyles ->
                let maybeWidth =
                    match styles with
                    | None ->
                        Log.Warn ("An Image is used with Size specified as FromStyles, but props?style is null. Source: {source}", source)
                        None
                    | Some nonNull ->
                        (ReactXP.LegacyStyles.Runtime.extractReactXpStyleValue "width" (nonNull :> obj :?> array<obj>)) :> obj :?> Option<int>
                LibClient.ServiceInstances.services().Image.MakeOptimizedUrl source maybeWidth |> Some
            | FromParentLayout maybeLayout ->
                match maybeLayout with
                | None        -> None
                | Some layout -> LibClient.ServiceInstances.services().Image.MakeOptimizedUrl source (Some layout.Width) |> Some

        match maybeUpdatedSource with
        | None -> noElement
        | Some uri ->
            let __props = createEmpty

            __props?source              <- ImageRN.makeSource uri headers
            __props?accessibilityLabel  <- accessibilityLabel
            __props?resizeMode          <- ImageRN.mapResizeMode resizeMode
            __props?androidResizeMethod <- androidResizeMethod
            __props?onLoad              <- ImageRN.wrapOnLoad onLoad
            __props?onError             <- onError
            __props?style               <- ImageRN.unboxStyles styles

            ImageRN.assignWebProps __props title

            ReactXP.RNSeam.createElement ReactXP.RNSeam.Image __props [||]
