// Public types (preserves LibClient.Components.Input.Image.* path for callers)
namespace LibClient.Components.Input

open LibClient
open LibLifeCycleTypes.File
open LibClient.Services.ImageService
open Rn.Styles

module Image =

    type SelectionMode = LibClient.Components.Input.File.SelectionMode
    let ReplacedExisting = SelectionMode.ReplacedExisting
    let AppendToExisting = SelectionMode.AppendToExisting


// Component extension
namespace LibClient.Components

open Fable.React
open LibClient
open LibClient.Accessibility
open LibClient.Components.Input.Image
open LibLifeCycleTypes.File
open LibClient.Services.ImageService
open Rn.Components
open Rn.Styles

[<AutoOpen>]
module Input_ImageComponent =

    [<RequireQualifiedAccess>]
    module private Styles =
        let imageThumbs =
            makeViewStyles {
                AlignItems.Center
                JustifyContent.Center
            }

    module private Helpers =
        let deleteFileFromIndices (selectedIndices: Set<uint32>) (files: list<File>) : list<File> =
            files
            |> List.indexed
            |> List.filter (fun (index, _) -> not (selectedIndices |> Set.contains (uint32 index)))
            |> List.map snd

        let filesToImageSources (files: list<File>) : Set<ImageSource> =
            files
            |> List.map (fun file -> file.ToDataUri |> ImageSource.ofUrl)
            |> Set.ofList

    type Constructors.LC.Input with
        [<Component>]
        static member Image(
                value:         list<File>,
                validity:      InputValidity,
                onChange:      Result<list<File>, string> -> unit,
                ?children:     ReactChildrenProp,
                ?showPreview:  bool,
                ?selectionMode: SelectionMode,
                ?maxFileCount: Positive.PositiveInteger,
                ?maxFileSize:  int<KB>,
                ?label:        string,
                ?styles:       array<ViewStyles>,
                ?testId:       string,
                ?key:          string,
                ?xLegacyStyles: List<Rn.LegacyStyles.RuntimeStyles>
            ) : ReactElement =
            children |> ignore
            key |> ignore

            let showPreview   = defaultArg showPreview true
            let selectionMode = defaultArg selectionMode ReplacedExisting

            let selectedIndicesHook = Hooks.useState Set.empty
            let selectedFilesHook   = Hooks.useState Set.empty

            let legacyViewStyles : array<ViewStyles> =
                match xLegacyStyles with
                | Some ls ->
                    match Rn.LegacyStyles.Runtime.findTopLevelBlockStyles ls with
                    | []     -> [||]
                    | styles -> [| Rn.LegacyStyles.Runtime.prepareStylesForPassingToRnComponent<ViewStyles> "Rn.Components.View" styles |]
                | None -> [||]

            let fileLegacyStyles =
                match xLegacyStyles with
                | Some ls ->
                    let s = Rn.LegacyStyles.Runtime.findApplicableStyles ls "image-input"
                    if s.IsEmpty then None else Some s
                | None -> None

            let resolvedTestId =
                testId
                |> Option.orElse (label |> Option.map (A11ySlug.testId "input"))
                |> Option.defaultValue "input-image"

            let toggleSelectedFilesForRemoval (index: uint32) (_e: ReactEvent.Action) =
                selectedIndicesHook.update (fun prev ->
                    let newSelectedIndices = prev.Toggle index
                    let selectedForRemoval =
                        value
                        |> List.except (Helpers.deleteFileFromIndices newSelectedIndices value)
                    selectedFilesHook.update (fun _ -> Helpers.filesToImageSources selectedForRemoval)
                    newSelectedIndices
                )

            let removeSelected (_e: ReactEvent.Action) =
                let remaining = Helpers.deleteFileFromIndices selectedIndicesHook.current value
                onChange (Ok remaining)
                selectedIndicesHook.update (fun _ -> Set.empty)
                selectedFilesHook.update (fun _ -> Set.empty)

            Rn.View(
                testId = resolvedTestId,
                styles =
                    [|
                        yield! legacyViewStyles
                        yield! styles |> Option.defaultValue [||]
                    |],
                children =
                    [|
                        if showPreview then
                            LC.Thumbs(
                                onPress  = (fun _ index e -> toggleSelectedFilesForRemoval index e),
                                testIdPrefix = resolvedTestId,
                                ``for``  = LC.Thumbs.For.Of(value |> List.map (fun file -> file.ToDataUri |> ImageSource.ofUrl)),
                                selected = selectedFilesHook.current,
                                styles   = [| Styles.imageThumbs |]
                            )
                        else
                            noElement

                        if selectedIndicesHook.current.IsNonempty then
                            LC.TextButton(
                                state = (LibClient.Components.TextButton.PropStateFactory.MakeLowLevel (LC.TextButton.Actionable removeSelected)),
                                label = "Remove Selected"
                            )
                        else
                            noElement

                        LC.Input.File(
                            onChange        = onChange,
                            selectionMode   = selectionMode,
                            acceptedTypes   = ([ LibClient.Components.Input.File.AnyImageFile ] |> Set.ofSeq),
                            ?maxFileSize    = maxFileSize,
                            ?maxFileCount   = maxFileCount,
                            validity        = validity,
                            value           = value,
                            ?xLegacyStyles  = fileLegacyStyles
                        )
                    |]
            )
