namespace LibClient.Components.Input

open Fable.Core
open Fable.Core.JsInterop
open LibLifeCycleTypes.File

module File =

    type LibLifeCycleFile     = LibLifeCycleTypes.File.File
    type LibLifeCycleFileData = LibLifeCycleTypes.File.FileData

    type AcceptedType =
    | FileNameExtension of string
    | MimeType of MimeType
    | AnyAudioFile
    | AnyVideoFile
    | AnyImageFile
    with
        member this.Value : string =
            match this with
            | FileNameExtension value -> value
            | MimeType mimeType       -> mimeType.Value
            | AnyAudioFile            -> "audio/*"
            | AnyVideoFile            -> "video/*"
            | AnyImageFile            -> "image/*"

    type SelectionMode =
    | ReplacedExisting
    | AppendToExisting


namespace LibClient.Components

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fable.React.Standard

open LibClient
open LibClient.Components.Input.File
open LibLifeCycleTypes.File

open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Input_FileComponent =

    module FRS = Fable.React.Standard
    module FRP = Fable.React.Props

    module LC =
        module Input =
            module File =
                type Theme = { InvalidColor: Color }

    open LC.Input.File

    type private FileContext = list<LibLifeCycleFile> * SelectionMode * Positive.PositiveInteger option * int<KB> option * (Result<list<LibLifeCycleFile>, string> -> unit) * (InputValidity -> unit)

    [<RequireQualifiedAccess>]
    module private Styles =
        let view = makeViewStyles { padding 5; marginTop 10; AlignItems.Center }
        // Key on CSS string (primitive), not Theme — fast-memoize uses reference equality on records.
        let viewInvalid =
            ViewStyles.Memoize (fun (invalidColorCss: string) ->
                makeViewStyles { borderColor (Color.InternalString invalidColorCss) })
        let textCenter = makeTextStyles { TextAlign.Center }
        let dragAndDropMessage = makeViewStyles { marginTop 20 }
        let invalidReason =
            TextStyles.Memoize (fun (invalidColorCss: string) ->
                makeTextStyles { TextAlign.Center; color (Color.InternalString invalidColorCss) })
        let infoMessage = makeTextStyles { TextAlign.Center; color Color.DevOrange }
        let messageContainer = makeViewStyles { marginTop 10 }

    [<RequireQualifiedAccess>]
    module private Helpers =
        [<Emit("Array.from(new Uint8Array($0))")>]
        let private arrayBufferToBytesArray (_: Fable.Core.JS.ArrayBuffer) : byte[] = jsNative

        let loadFile (maxFileSize: int<KB> option) (file: Browser.Types.File) : Async<Result<LibLifeCycleFile, string>> =
            let deferred = LibLangFsharp.Deferred<Result<LibLifeCycleFile, string>>()
            match (MimeType.ofString file.``type``, maxFileSize) with
            | (None, _) -> deferred.Resolve (Error $"Unknown file type: %s{file.``type``}")
            | (_, Some maxFileSize) when (asB file.size) > (kBToB maxFileSize) -> deferred.Resolve (Error "File is too large")
            | (Some mimeType, _) ->
                let reader = Browser.Dom.FileReader.Create()
                reader.onload <- (fun (event: Browser.Types.Event) ->
                    let arrayBuffer: JS.ArrayBuffer = event.target?result
                    let bytes = arrayBufferToBytesArray arrayBuffer
                    deferred.Resolve (Ok { MimeType = mimeType; Data = LibLifeCycleFileData.Bytes bytes })
                )
                reader.readAsArrayBuffer file
            deferred.Value

        let browserFilesFromDataTransfer (dataTransfer: Browser.Types.DataTransfer) : seq<Browser.Types.File> =
            if not (isNullOrUndefined dataTransfer.items) then
                seq {0 .. dataTransfer.items.length - 1} |> Seq.filterMap (fun i ->
                    match dataTransfer.items.[i].kind with | "file" -> Some (dataTransfer.items.[i].getAsFile()) | _ -> None)
            elif not (isNullOrUndefined dataTransfer.files) then
                seq {0 .. dataTransfer.files.length - 1} |> Seq.map (fun i -> dataTransfer.files.[i])
            else Seq.empty

        let browserFilesFromInput (fileInput: Browser.Types.HTMLInputElement) : seq<Browser.Types.File> =
            seq {0 .. fileInput.files.length - 1} |> Seq.map (fun i -> fileInput.files.[i])

        let loadFilesFromBrowser (value: list<LibLifeCycleFile>) selectionMode maxFileCount maxFileSize onChange (setInternalValidity: InputValidity -> unit) (browserFiles: seq<Browser.Types.File>) =
            async {
                setInternalValidity InputValidity.Valid
                let! results = browserFiles |> Seq.map (loadFile maxFileSize) |> Async.Parallel
                let mappedResults : Result<list<LibLifeCycleFile>, string> =
                    Result.liftList (List.ofArray results)
                    |> Result.mapError (fun errors ->
                        let msg = String.concat "\n" errors
                        setInternalValidity (InputValidity.Invalid msg)
                        msg)
                let allFiles =
                    match (selectionMode, mappedResults) with
                    | (AppendToExisting, Ok result) -> Ok (value @ result)
                    | _ -> mappedResults
                match (InputValidity.Valid, allFiles, maxFileCount) with
                | (InputValidity.Valid, Ok loadedFiles, Some (maxCount: Positive.PositiveInteger)) when loadedFiles.Length > maxCount.Value ->
                    setInternalValidity (InputValidity.Invalid "Too many files")
                | (InputValidity.Valid, Ok _, _) -> onChange allFiles
                | _ -> Noop
            } |> startSafely

    let private legacyTopLevelStyles xLegacyStyles =
        match xLegacyStyles with
        | Some ls ->
            match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
            | [] -> [||]
            | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
        | None -> [||]

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member File(
                value: list<LibLifeCycleFile>,
                validity: InputValidity,
                onChange: Result<list<LibLifeCycleFile>, string> -> unit,
                ?children: ReactChildrenProp,
                ?acceptedTypes: Set<AcceptedType>,
                ?selectionMode: SelectionMode,
                ?maxFileCount: Positive.PositiveInteger,
                ?maxFileSize: int<KB>,
                ?styles: array<ViewStyles>,
                ?theme: Theme -> Theme,
                ?key: string,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            children |> ignore
            key |> ignore

            let acceptedTypes = defaultArg acceptedTypes Set.empty
            let selectionMode = defaultArg selectionMode ReplacedExisting
            let theTheme = Themes.GetMaybeUpdatedWith theme
            let internalValidityHook = Hooks.useState InputValidity.Valid

            let valueLength = value.Length
            Hooks.useEffect(
                (fun () ->
                    internalValidityHook.update (fun current ->
                        match current with
                        | InputValidity.Valid -> current
                        | _ -> InputValidity.Valid)),
                [| box valueLength |]
            )

            let contextRef = Hooks.useRef<FileContext>((value, selectionMode, maxFileCount, maxFileSize, onChange, internalValidityHook.update))
            contextRef.current <- (value, selectionMode, maxFileCount, maxFileSize, onChange, internalValidityHook.update)

            let loadFiles browserFiles =
                let (value, selectionMode, maxFileCount, maxFileSize, onChange, setInternalValidity) = contextRef.current
                Helpers.loadFilesFromBrowser value selectionMode maxFileCount maxFileSize onChange setInternalValidity browserFiles

            let onSelectPress maybeInput (_e: ReactEvent.Action) =
                maybeInput |> Option.sideEffect (fun el -> el?click())

            let onInputInitialize =
                Hooks.useMemo(
                    (fun () ->
                        fun (input: Browser.Types.Element) ->
                            let fileInput = input :> obj :?> Browser.Types.HTMLInputElement
                            input.addEventListener("change", fun (_: Browser.Types.Event) ->
                                Helpers.browserFilesFromInput fileInput |> loadFiles
                            )
                    ),
                    [| |]
                )

            let onDropZoneInitialize =
                Hooks.useMemo(
                    (fun () ->
                        fun (div: Browser.Types.Element) ->
                            div.addEventListener("dragover", fun (e: Browser.Types.Event) -> e.preventDefault())
                            div.addEventListener("drop", fun (e: Browser.Types.Event) ->
                                e.preventDefault()
                                let maybeDataTransfer: Option<Browser.Types.DataTransfer> = e?dataTransfer
                                maybeDataTransfer |> Option.sideEffect (fun dt ->
                                    Helpers.browserFilesFromDataTransfer dt |> loadFiles
                                )
                            )
                    ),
                    [| |]
                )

            let acceptValue = acceptedTypes |> Set.map (fun item -> item.Value) |> String.concat ", "
            let combinedInvalidReason =
                if internalValidityHook.current.InvalidReason.IsSome then internalValidityHook.current.InvalidReason
                else validity.InvalidReason
            let isInvalid =
                internalValidityHook.current.IsInvalid || validity.IsInvalid || validity = InputValidity.Missing

            Rn.View(
                styles = [| Styles.view; if isInvalid then Styles.viewInvalid theTheme.InvalidColor.ToCssString; yield! legacyTopLevelStyles xLegacyStyles; yield! defaultArg styles [||] |],
                children = [|
                    LC.With.RefDom(
                        onInitialize = onDropZoneInitialize,
                        ``with`` = fun (bindDivRef, _) ->
                            FRS.div [ Ref bindDivRef ] [|
                                LC.With.RefDom(
                                    onInitialize = onInputInitialize,
                                    ``with`` = fun (bindRef, maybeFileInputElement) ->
                                        [|
                                            FRS.input [ FRP.Type "file"; FRP.Multiple true; FRP.Hidden true; FRP.Ref bindRef; FRP.Accept acceptValue ]
                                            LC.Button(
                                                label = "Select File",
                                                state = LibClient.Components.Button.PropStateFactory.MakeLowLevel (
                                                    LibClient.Components.Button.Actionable (onSelectPress maybeFileInputElement)
                                                )
                                            )
                                        |]
                                        |> castAsElement
                                )
                                Rn.View(
                                    styles = [| Styles.dragAndDropMessage |],
                                    children = [|
                                        LC.LegacyText(
                                            styles = [| Styles.textCenter |],
                                            children = [|
                                                match maxFileCount with
                                                | Some c when c.Value = 1 -> makeTextNode2 (Some "LibClient.Components.LegacyText") "or drag and drop file here"
                                                | _ -> makeTextNode2 (Some "LibClient.Components.LegacyText") "or drag and drop files here"
                                            |]
                                        )
                                    |]
                                )
                                Rn.View(
                                    children = [|
                                        LC.LegacyText(
                                            styles = [| Styles.textCenter |],
                                            children = [|
                                                match (maxFileCount, maxFileSize) with
                                                | (Some c, Some s) when c.Value > 1 -> makeTextNode2 (Some "LibClient.Components.LegacyText") $"Maximum {c.Value} files each below {kBToMB s} MB"
                                                | (Some c, None) when c.Value > 1 -> makeTextNode2 (Some "LibClient.Components.LegacyText") $"Maximum {c.Value} files"
                                                | (_, Some s) -> makeTextNode2 (Some "LibClient.Components.LegacyText") $"Size below {kBToMB s} MB"
                                                | _ -> noElement
                                            |]
                                        )
                                    |]
                                )
                                Rn.View(
                                    styles = [| Styles.messageContainer |],
                                    children = [|
                                        Rn.View(
                                            children = [|
                                                if value.Length = 1 then
                                                    LC.LegacyText(
                                                        styles = [| Styles.infoMessage |],
                                                        children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") $"{value.Length} file selected" |]
                                                    )
                                                elif value.Length > 1 then
                                                    LC.LegacyText(
                                                        styles = [| Styles.infoMessage |],
                                                        children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") $"{value.Length} files selected" |]
                                                    )
                                                else noElement
                                            |]
                                        )
                                        combinedInvalidReason
                                        |> Option.map (fun reason ->
                                            Rn.View(
                                                children = [|
                                                    LC.LegacyText(
                                                        styles = [| Styles.invalidReason theTheme.InvalidColor.ToCssString |],
                                                        children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") reason |]
                                                    )
                                                |]
                                            )
                                        )
                                        |> Option.getOrElse noElement
                                        if validity = InputValidity.Missing then
                                            Rn.View(
                                                children = [|
                                                    LC.LegacyText(
                                                        styles = [| Styles.invalidReason theTheme.InvalidColor.ToCssString |],
                                                        children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") "This field is required" |]
                                                    )
                                                |]
                                            )
                                        else noElement
                                    |]
                                )
                            |]
                    )
                |]
            )
