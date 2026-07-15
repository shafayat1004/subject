[<AutoOpen>]
module ThirdParty.ImagePicker.Components.Base

open Fable.React
open LibClient
open LibClient.Components
open LibLifeCycleTypes.File
open Rn.Styles
open ThirdParty.ImagePicker.Components.Constructors
open ThirdParty.ImagePicker.Components.Native.ImagePicker

type SelectionMode = LibClient.Components.Input.File.SelectionMode
let ReplacedExisting = SelectionMode.ReplacedExisting
let AppendToExisting = SelectionMode.AppendToExisting

type ImagePicker with
    [<Component>]
    static member Base(
            value:          list<File>,
            validity:       InputValidity,
            onChange:       Result<list<File>, string> -> unit,
            ?maxFileCount:  Positive.PositiveInteger,
            ?maxFileSize:   int<KB>,
            ?showPreview:   bool,
            ?selectionMode: SelectionMode,
            ?styles:        array<ViewStyles>,
            ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>,
            ?key:           string
        ) : ReactElement =
        ignore key
        xLegacyStyles |> ignore

        let showPreview = defaultArg showPreview true
        let selectionMode = defaultArg selectionMode ReplacedExisting

        #if EGGSHELL_PLATFORM_IS_WEB
        LC.Input.Image(
            value         = value,
            validity      = validity,
            onChange      = onChange,
            ?maxFileCount = maxFileCount,
            ?maxFileSize  = maxFileSize,
            showPreview   = showPreview,
            selectionMode = selectionMode,
            ?styles       = styles
        )
        #else
        ImagePicker.Native.ImagePicker(
            value          = value,
            validity       = validity,
            onChange       = onChange,
            ?maxFileCount  = maxFileCount,
            ?maxFileSize   = maxFileSize,
            showPreview    = showPreview,
            selectionMode  = selectionMode,
            ?styles        = styles,
            ?xLegacyStyles = xLegacyStyles
        )
        #endif
