[<AutoOpen>]
module AppEggShellGallery.Components.ScrapedComponentProps

open Fable.React
open LibClient
open LibRenderDSL.Types
open Scraping.Types

let getScrapedPropsData (fullyQualifiedName: string) : ComponentProps.Data =
    let propsDataResult : Result<ScrapeResult, string> = AppEggShellGallery.ScrapedData.Props.propsDataResult

    let fields : Result<List<TaggedRecordField>, string> =
        propsDataResult
        |> Result.bind (fun propsData -> Ok propsData.Results)
        |> Result.bind (fun scrapeResult ->
            match scrapeResult.TryFind fullyQualifiedName with
            | Some value -> value |> Result.mapError (sprintf "%A")
            | None       -> Error (sprintf "Fully Qualified Name not found: %s" fullyQualifiedName)
        )
        |> Result.bind (fun taggedRecordType -> Ok taggedRecordType.Fields)

    let maybeScrapeErrors : Option<NonemptyList<ScrapeError>> =
        propsDataResult
        |> Result.toOption
        |> Option.flatMap (fun scrapeData -> NonemptyList.ofList scrapeData.Errors)

    {
        Fields            = Choice1Of2 fields
        MaybeScrapeErrors = maybeScrapeErrors
    }

type AppEggShellGallery.Components.Constructors.Ui with
    [<Component>]
    static member ScrapedComponentProps(
            fullyQualifiedName: string,
            ?children:          ReactChildrenProp,
            ?heading:          string,
            ?key:              string,
            ?xLegacyStyles:    List<ReactXP.LegacyStyles.RuntimeStyles>
        ) : ReactElement =
        key |> ignore
        children |> ignore
        xLegacyStyles |> ignore

        Ui.ComponentProps(
            data = getScrapedPropsData fullyQualifiedName,
            ?heading = heading
        )
