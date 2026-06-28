[<AutoOpen>]
module ThirdParty.ImagePicker.Components.Native.ImagePicker

open Fable.React
open LibClient
open LibClient.Components
open LibLifeCycleTypes.File
open Fable.Core
open ThirdParty.ImagePicker.Components.ReactNativeImagePicker
open ReactXP.Components
open ReactXP.Styles
open ThirdParty.ImagePicker.Components.Constructors

type SelectionMode = LibClient.Components.Input.File.SelectionMode
let ReplacedExisting = SelectionMode.ReplacedExisting
let AppendToExisting = SelectionMode.AppendToExisting

[<Global>]
let private atob (_encodedString: string): string = jsNative

[<Global>]
let private Uint8Array (_length: int): obj = jsNative

module private Styles =
    let imageThumbs =
        makeViewStyles {
            AlignItems.Center
            JustifyContent.Center
        }

    let view =
        makeViewStyles {
            padding 5
            marginTop 10
            AlignItems.Center
        }

    let textCenter = makeTextStyles { TextAlign.Center }

    let constrainMessage = makeViewStyles { marginTop 10 }

    let invalidReason =
        makeTextStyles {
            TextAlign.Center
            color Color.DevRed
        }

    let viewInvalid =
        makeViewStyles {
            padding 5
            marginTop 10
            AlignItems.Center
            borderColor Color.DevRed
        }

type ThirdParty.ImagePicker.Components.Constructors.ImagePicker.Native with
    [<Component>]
    static member ImagePicker(
            value:         list<File>,
            validity:      InputValidity,
            onChange:      Result<list<File>, string> -> unit,
            ?maxFileCount: Positive.PositiveInteger,
            ?maxFileSize:  int<KB>,
            ?showPreview:  bool,
            ?selectionMode: SelectionMode,
            ?styles:       array<ViewStyles>,
            ?xLegacyStyles: List<ReactXP.LegacyStyles.RuntimeStyles>,
            ?key:          string
        ) : ReactElement =
        ignore key
        xLegacyStyles |> ignore

        let showPreview = defaultArg showPreview true
        let selectionMode = defaultArg selectionMode ReplacedExisting

        let internalValidityHook = Hooks.useState InputValidity.Valid

        Hooks.useEffect(
            (fun () -> internalValidityHook.update InputValidity.Valid),
            [| box value; box validity |]
        )

        let tryAssetToFile (maybeMaxFileSize: Option<int<KB>>) (asset: Asset) : Result<File, string> =
            match (MimeType.ofString asset.Type, maybeMaxFileSize) with
            | (None, _) -> Error (sprintf "Unknown file type: %s" asset.Type)
            | (_, Some maxFileSize) when asset.FileSize > kBToB maxFileSize -> Error "File is too large"
            | (Some mimeType, _) ->
                {
                    MimeType = mimeType
                    Data     = FileData.Base64 (asset.Base64, asset.FileSize)
                }
                |> Ok

        let loadFiles (assets: list<Asset>) : unit =
            async {
                internalValidityHook.update InputValidity.Valid

                let mappedResults =
                    assets
                    |> List.map (tryAssetToFile maxFileSize)
                    |> Result.liftList
                    |> Result.mapError (fun errors ->
                        let invalidReasons = String.concat "\n" errors
                        internalValidityHook.update (InputValidity.Invalid invalidReasons)
                        invalidReasons
                    )

                let allFiles =
                    match (selectionMode, mappedResults) with
                    | (SelectionMode.AppendToExisting, Ok result) -> Ok (value @ result)
                    | _                                           -> mappedResults

                match (internalValidityHook.current, allFiles, maxFileCount) with
                | (InputValidity.Valid, Ok files, Some maxCount) when files.Length > maxCount.Value ->
                    internalValidityHook.update (InputValidity.Invalid "Too many files")
                | (InputValidity.Valid, Ok _, _) ->
                    onChange allFiles
                | _ ->
                    Noop
            } |> startSafely

        let selectImage (maybeAssets: Option<list<Asset>>) : unit =
            match maybeAssets with
            | Some assets -> loadFiles assets
            | None -> ()

        let mergedViewStyles =
            match validity, internalValidityHook.current with
            | InputValidity.Invalid _, _
            | _, InputValidity.Invalid _ -> [| Styles.viewInvalid; yield! Option.defaultValue [||] styles |]
            | _ -> [| Styles.view; yield! Option.defaultValue [||] styles |]

        let invalidReason =
            match internalValidityHook.current.InvalidReason with
            | Some reason -> Some reason
            | None -> validity.InvalidReason

        RX.View(
            children =
                [|
                    if showPreview then
                        LC.Thumbs(
                            ``for`` =
                                (Thumbs.PropForFactory.Make(
                                    value
                                    |> List.map (fun file ->
                                        file.ToDataUri |> LibClient.Services.ImageService.ImageSource.ofUrl)
                                )),
                            styles = [| Styles.imageThumbs |]
                        )
                    else
                        noElement

                    RX.View(
                        styles = mergedViewStyles,
                        children =
                            [|
                                ImagePicker.Native.ReactNativeImagePicker(onImageSelect = selectImage)

                                RX.View(
                                    styles = [| Styles.constrainMessage |],
                                    children =
                                        [|
                                            LC.LegacyText(
                                                styles = [| Styles.textCenter |],
                                                children =
                                                    [|
                                                        match (maxFileCount, maxFileSize) with
                                                        | (Some maxCount, Some maxSize) when maxCount.Value > 1 ->
                                                            makeTextNode2 (Some "LibClient.Components.LegacyText") (
                                                                sprintf "Maximum %i files each below %A MB"
                                                                    maxCount.Value
                                                                    (kBToMB maxSize))
                                                        | (Some maxCount, None) when maxCount.Value > 1 ->
                                                            makeTextNode2 (Some "LibClient.Components.LegacyText") (sprintf "Maximum %i files" maxCount.Value)
                                                        | (_, Some maxSize) ->
                                                            makeTextNode2 (Some "LibClient.Components.LegacyText") (sprintf "Size below %A MB" (kBToMB maxSize))
                                                        | _ -> noElement
                                                    |]
                                            )
                                        |]
                                )

                                RX.View(
                                    children =
                                        [|
                                            if value.Length = 1 then
                                                LC.LegacyText(
                                                    styles = [| Styles.textCenter |],
                                                    children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") (sprintf "%i file selected" value.Length) |]
                                                )
                                            else
                                                noElement

                                            if value.Length > 1 then
                                                LC.LegacyText(
                                                    styles = [| Styles.textCenter |],
                                                    children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") (sprintf "%i files selected" value.Length) |]
                                                )
                                            else
                                                noElement
                                        |]
                                )

                                match invalidReason with
                                | Some reason ->
                                    RX.View(
                                        children =
                                            [|
                                                LC.LegacyText(
                                                    styles = [| Styles.invalidReason |],
                                                    children = [| makeTextNode2 (Some "LibClient.Components.LegacyText") reason |]
                                                )
                                            |]
                                    )
                                | None -> noElement
                            |]
                    )
                |]
        )
