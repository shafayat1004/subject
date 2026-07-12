namespace LibClient.Components.Input

open Fable.Core
open Fable.Core.JsInterop
open LibLifeCycleTypes.File

module NamedFile =

    type LibLifeCycleNamedFile = LibLifeCycleTypes.File.NamedFile
    type LibLifeCycleFileData  = LibLifeCycleTypes.File.FileData

    type AcceptedType =
    | FileNameExtension of string
    | MimeType          of MimeType
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

    let constrainMessage (maxFileCount: Option<Positive.PositiveInteger>) (maxFileSize: Option<int<KB>>) (maxTotalFileSize: Option<int<KB>>) =
        [
            maxFileCount     |> Option.map (fun c -> $"Up to {c.Value} files.")
            maxFileSize      |> Option.map (fun s -> $"{kBToMB s} MB per file.")
            maxTotalFileSize |> Option.map (fun s -> $"{kBToMB s} MB total.")
        ]
        |> Seq.choose id
        |> String.concat " "
        |> NonemptyString.ofString

    let getPrintableAsciiChars (input: string) : string =
        input |> Seq.filter (fun c -> c >= ' ' && c <= '~') |> System.String.Concat


namespace LibClient.Components

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open Fable.React.Standard

open LibClient
open LibClient.Components.Input.NamedFile
open LibClient.Responsive
open LibLifeCycleTypes.File

open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Input_NamedFileComponent =

    module FRS = Fable.React.Standard
    module FRP = Fable.React.Props

    module LC =
        module Input =
            module NamedFile =
                type Theme = { InvalidColor: Color; DropZoneBorderColor: Color }

    open LC.Input.NamedFile

    type private NamedFileContext = list<LibLifeCycleNamedFile> * SelectionMode * Positive.PositiveInteger option * int<KB> option * int<KB> option * (Result<list<LibLifeCycleNamedFile>, string> -> unit) * (InputValidity -> unit)

    [<RequireQualifiedAccess>]
    module private Styles =
        let selectFile = makeViewStyles { paddingHorizontal 60 }
        let view (_: Theme) = makeViewStyles { flex 1; padding 5; marginTop 10; AlignItems.Center }
        let viewDesktop (theme: Theme) = makeViewStyles { borderStyle BorderStyle.Dashed; border 2 theme.DropZoneBorderColor; borderRadius 6 }
        let viewInvalid (theme: Theme) = makeViewStyles { borderColor theme.InvalidColor }
        let textCenter = makeTextStyles { TextAlign.Center }
        let dragAndDropMessage = makeViewStyles { marginTop 20 }
        let invalidReason (theme: Theme) = makeTextStyles { TextAlign.Center; color theme.InvalidColor }
        let infoMessage = makeTextStyles { TextAlign.Center; color Color.DevOrange }
        let messageContainer = makeViewStyles { marginTop 10 }

    [<RequireQualifiedAccess>]
    module private Helpers =
        [<Emit("Array.from(new Uint8Array($0))")>]
        let private arrayBufferToBytesArray (_: Fable.Core.JS.ArrayBuffer) : byte[] = jsNative

        let loadFile (maxFileSize: int<KB> option) (file: Browser.Types.File) : Async<Result<LibLifeCycleNamedFile, string>> =
            let deferred = LibLangFsharp.Deferred<Result<LibLifeCycleNamedFile, string>>()
            match (MimeType.ofString file.``type``, maxFileSize) with
            | (None, _) -> deferred.Resolve (Error $"Unknown file type: %s{file.``type``}")
            | (_, Some maxFileSize) when (asB file.size) > (kBToB maxFileSize) -> deferred.Resolve (Error $"{file.name} is too large")
            | (Some mimeType, _) ->
                let reader = Browser.Dom.FileReader.Create()
                reader.onload <- (fun (event: Browser.Types.Event) ->
                    let arrayBuffer: JS.ArrayBuffer = event.target?result
                    let bytes = arrayBufferToBytesArray arrayBuffer
                    deferred.Resolve (Ok {
                        Name = file.name |> getPrintableAsciiChars |> NonemptyString.ofStringWithDefault "Untitled"
                        File = { MimeType = mimeType; Data = LibLifeCycleFileData.Bytes bytes }
                    })
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

        let browserFilesFromClipboard (items: Browser.Types.DataTransferItemList) : seq<Browser.Types.File> =
            if isNullOrUndefined items then Seq.empty
            else seq {0 .. items.length - 1} |> Seq.filterMap (fun i -> if items.[i].kind = "file" then Some (items.[i].getAsFile()) else None)

        let loadFilesFromBrowser (value: list<LibLifeCycleNamedFile>) selectionMode maxFileCount maxFileSize maxTotalFileSize onChange (setInternalValidity: InputValidity -> unit) (browserFiles: seq<Browser.Types.File>) =
            let browserFiles = browserFiles |> Seq.toArray
            let totalExisting = value |> Seq.map (fun f -> f.File.Bytes.Length) |> Seq.fold (+) 0
            let totalNew = browserFiles |> Seq.map (fun f -> f.size) |> Seq.fold (+) 0
            match maxFileCount, maxTotalFileSize with
            | Some (c: Positive.PositiveInteger), _ when browserFiles.Length > c.Value -> setInternalValidity (InputValidity.Invalid "Too many files")
            | _, Some t when (asB totalNew) > (kBToB t) -> setInternalValidity (InputValidity.Invalid "Total file size limit exceeded")
            | _, Some t when selectionMode = AppendToExisting && ((asB totalNew) + (asB totalExisting)) > (kBToB t) ->
                setInternalValidity (InputValidity.Invalid "Total file size limit exceeded")
            | _ ->
                async {
                    setInternalValidity InputValidity.Valid
                    let! results = browserFiles |> Seq.map (loadFile maxFileSize) |> Async.Parallel
                    let mapped =
                        Result.liftList (List.ofArray results)
                        |> Result.mapError (fun errors ->
                            let msg = String.concat "\n" errors
                            setInternalValidity (InputValidity.Invalid msg)
                            msg)
                    let allFiles : Result<list<LibLifeCycleNamedFile>, string> =
                        match selectionMode, mapped with | AppendToExisting, Ok r -> Ok (value @ r) | _ -> mapped
                    match allFiles with | Ok namedFiles -> onChange (Ok namedFiles) | Error _ -> Noop
                } |> startSafely

    let private legacyTopLevelStyles xLegacyStyles =
        match xLegacyStyles with
        | Some ls ->
            match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
            | []     -> [||]
            | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
        | None -> [||]

    type LibClient.Components.Constructors.LC.Input with
        [<Component>]
        static member NamedFile(
                value:             list<LibLifeCycleNamedFile>,
                validity:          InputValidity,
                onChange:          Result<list<LibLifeCycleNamedFile>, string> -> unit,
                ?children:         ReactChildrenProp,
                ?acceptedTypes:    Set<AcceptedType>,
                ?selectionMode:    SelectionMode,
                ?maxFileCount:     Positive.PositiveInteger,
                ?maxFileSize:      int<KB>,
                ?maxTotalFileSize: int<KB>,
                ?styles:           array<ViewStyles>,
                ?theme:            Theme -> Theme,
                ?key:              string,
                ?xLegacyStyles:    List<Rn.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            children |> ignore
            key      |> ignore

            let acceptedTypes = defaultArg acceptedTypes Set.empty
            let selectionMode = defaultArg selectionMode ReplacedExisting
            let theTheme = Themes.GetMaybeUpdatedWith theme
            let internalValidityHook = Hooks.useState InputValidity.Valid

            Hooks.useEffect(
                (fun () -> internalValidityHook.update (fun (_: InputValidity) -> InputValidity.Valid)),
                [| value; validity; acceptedTypes; selectionMode; maxFileCount; maxFileSize; maxTotalFileSize |]
            )

            let contextRef = Hooks.useRef<NamedFileContext>((value, selectionMode, maxFileCount, maxFileSize, maxTotalFileSize, onChange, internalValidityHook.update))
            contextRef.current <- (value, selectionMode, maxFileCount, maxFileSize, maxTotalFileSize, onChange, internalValidityHook.update)

            let loadFiles browserFiles =
                let (value, selectionMode, maxFileCount, maxFileSize, maxTotalFileSize, onChange, setInternalValidity) = contextRef.current
                Helpers.loadFilesFromBrowser value selectionMode maxFileCount maxFileSize maxTotalFileSize onChange setInternalValidity browserFiles

            let onSelectPress maybeInput (_e: ReactEvent.Action) = maybeInput |> Option.sideEffect (fun el -> el?click())

            let onInputInitialize =
                Hooks.useMemo((fun () -> fun (input: Browser.Types.Element) ->
                    let fileInput = input :> obj :?> Browser.Types.HTMLInputElement
                    input.addEventListener("change", fun (_: Browser.Types.Event) -> Helpers.browserFilesFromInput fileInput |> loadFiles)), [| |])

            let onDropZoneInitialize =
                Hooks.useMemo((fun () -> fun (div: Browser.Types.Element) ->
                    div.addEventListener("dragover", fun (e: Browser.Types.Event) -> e.preventDefault())
                    div.addEventListener("drop", fun (e:     Browser.Types.Event) ->
                        e.preventDefault()
                        let maybeDataTransfer: Option<Browser.Types.DataTransfer> = e?dataTransfer
                        maybeDataTransfer |> Option.sideEffect (fun dt -> Helpers.browserFilesFromDataTransfer dt |> loadFiles))
                    div.addEventListener("paste", fun (e: Browser.Types.Event) ->
                        e.preventDefault()
                        Helpers.browserFilesFromClipboard ((e :?> Browser.Types.ClipboardEvent).clipboardData.items) |> loadFiles)), [| |])

            let acceptValue = acceptedTypes |> Set.map (fun item -> item.Value) |> String.concat ", "
            let combinedInvalidReason =
                if internalValidityHook.current.InvalidReason.IsSome then internalValidityHook.current.InvalidReason else validity.InvalidReason
            let isInvalid = internalValidityHook.current.IsInvalid || validity.IsInvalid || validity = InputValidity.Missing

            LC.With.ScreenSize(``with`` = fun screenSize ->
                Rn.View(
                    styles = [|
                        Styles.view theTheme
                        if screenSize = ScreenSize.Desktop then Styles.viewDesktop theTheme
                        if isInvalid then Styles.viewInvalid theTheme
                        yield! legacyTopLevelStyles xLegacyStyles
                        yield! defaultArg styles [||]
                    |],
                    children = [|
                        LC.With.RefDom(onInitialize = onDropZoneInitialize, ``with`` = fun (bindDivRef, _) ->
                            FRS.div [ Ref bindDivRef; Style [ CSSProp.Width "100%"; CSSProp.Height "100%" ] ] [|
                                LC.With.RefDom(onInitialize = onInputInitialize, ``with`` = fun (bindRef, maybeFileInputElement) ->
                                    [|
                                        FRS.input [ FRP.Type "file"; FRP.Multiple true; FRP.Hidden true; FRP.Ref bindRef; FRP.Accept acceptValue ]
                                        LC.Buttons(align = Align.Center, children = [|
                                            LC.Button(label = "Select File", styles = [| Styles.selectFile |],
                                                state = LibClient.Components.Button.PropStateFactory.MakeLowLevel (
                                                    LibClient.Components.Button.Actionable (onSelectPress maybeFileInputElement)))
                                        |])
                                    |] |> castAsElement)
                                Rn.View(styles = [| Styles.dragAndDropMessage |], children = [|
                                    LC.LegacyText(styles = [| Styles.textCenter |], children = [|
                                        match maxFileCount with
                                        | Some c when c.Value = 1 -> makeTextNode2 (Some "LibClient.Components.LegacyText") "Paste or drag and drop file here"
                                        | _                       -> makeTextNode2 (Some "LibClient.Components.LegacyText") "Paste or drag and drop files here"
                                    |])
                                |])
                                Rn.View(children = [|
                                    constrainMessage maxFileCount maxFileSize maxTotalFileSize
                                    |> Option.map (fun m -> LC.LegacyText(styles = [| Styles.textCenter |],
                                        children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") m.Value |]))
                                    |> Option.getOrElse noElement
                                |])
                                Rn.View(styles = [| Styles.messageContainer |], children = [|
                                    Rn.View(children = [|
                                        if value.Length = 1 then LC.LegacyText(styles = [| Styles.infoMessage |],
                                            children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") $"{value.Length} file selected" |])
                                        elif value.Length > 1 then LC.LegacyText(styles = [| Styles.infoMessage |],
                                            children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") $"{value.Length} files selected" |])
                                        else noElement
                                    |])
                                    combinedInvalidReason |> Option.map (fun reason -> Rn.View(children = [|
                                        LC.LegacyText(styles = [| Styles.invalidReason theTheme |],
                                            children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") reason |])
                                    |])) |> Option.getOrElse noElement
                                    if validity = InputValidity.Missing then Rn.View(children = [|
                                        LC.LegacyText(styles = [| Styles.invalidReason theTheme |],
                                            children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") "This field is required" |])
                                    |]) else noElement
                                |])
                            |])
                    |]
                )
            )
