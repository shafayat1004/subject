[<AutoOpen>]
module Rn.LegacyStyles.Designtime

open Rn.Styles.Types
open Rn.LegacyStyles
open Fable.Core.JsInterop
open LibClient

// it's not recommended to override the && operator, but we're building a DSL here
#nowarn "0086"


(*
    Requirements:
    * To leaf Rn components, we need to pass either a single object
      of style names to values, or an array of such objects
    * To non-leaf components, we need to pass style sheets, i.e. mappings
      from selector to aforementioned style name to value objects
    * transformation from whatever helper types to raw Rn types needs
      to happen as early and infrequently as possible, i.e. only at style
      sheet definition time, not application time. Ideally it would actually
      be at compile time (though reliance on runtime values won't allow for this).
 *)

let rec flattenStyleRules (rules: seq<RuleFunctionReturnedStyleRules>) : List<TypedRnStyleRule> =
    let rec loop (acc: List<TypedRnStyleRule>) (remainingInput: List<RuleFunctionReturnedStyleRules>) =
        match remainingInput with
        | [] -> List.rev acc
        | r :: rs ->
            match r with
            | RuleFunctionReturnedStyleRules.Many currRules -> loop (List.append (flattenStyleRules currRules) acc) rs
            | RuleFunctionReturnedStyleRules.One  currRule  -> loop (currRule :: acc) rs

    loop [] (rules |> List.ofSeq)


let rec private flattenStyleRulesToArrayHelper (rules: array<RuleFunctionReturnedStyleRules>, acc: array<TypedRnStyleRule>) : unit =
    for i in 0 .. rules.Length - 1 do
        match rules[i] with
        | RuleFunctionReturnedStyleRules.Many currRules ->
            flattenStyleRulesToArrayHelper (currRules, acc)
        | RuleFunctionReturnedStyleRules.One  currRule  ->
            acc[acc.Length] <- currRule

let rec flattenStyleRulesToArray (rules: array<RuleFunctionReturnedStyleRules>) : array<TypedRnStyleRule> =
   let result: array<TypedRnStyleRule> = [||]
   flattenStyleRulesToArrayHelper (rules, result)
   result

let private createRnAnimatedViewStyleObject (typedValues: seq<TypedRnStyleRule>) : RnStyleRulesObject =
    let rawValues = typedValues |> Seq.map fst
    Rn.RnPrimitives.createAnimatedViewStyle(createObj (rawValues :?> seq<string * obj>))

let processDynamicStyles (rawValues: seq<RuleFunctionReturnedStyleRules>) : List<RuntimeStyles> =
    rawValues
    |> flattenStyleRules
    |> LazilyCreatedRnStyleObject
    |> RuntimeStyles.StaticRules
    |> List.ofOneItem

let processDynamicAniStyles (runtimeStyles: List<RuntimeStyles>) : List<RuntimeStyles> =
    runtimeStyles

let aniRules (rawValues: seq<RuleFunctionReturnedStyleRules>) : RnStyleRulesObject =
    rawValues
    |> flattenStyleRules
    |> createRnAnimatedViewStyleObject

let flattenBuildingBlocks(blocks: List<ISheetBuildingBlock>) : List<Selector * Styles> =
    blocks
    |> List.collect (fun block ->
        block.Match
            (fun one  -> [one.ToTuple])
            (fun many -> many.Blocks)
    )

let rec private flattenAndBuildRnInternalRepresentation (rawValues: seq<RuleFunctionReturnedStyleRules>) : Styles =
    rawValues
    |> flattenStyleRules
    |> LazilyCreatedRnStyleObject
    |> Styles.Rules


let rec private processPairs (selectorToStylesPairs: List<Selector * Styles>) : RuntimeStyles =
    let processed =
        selectorToStylesPairs
        |> List.map (fun (selector, styles) ->
            let selectorSet = selector.Split([|"&&"|], System.StringSplitOptions.None) |> Seq.map (fun s -> s.Trim()) |> Set.ofSeq
            (selectorSet, processStyles styles)
        )

    RuntimeStyles.Sheet processed

and processStyles (styles: Styles) : RuntimeStyles =
    match styles with
    | Styles.Sheet sheet         -> processPairs sheet
    | Styles.Rules rnRulesObject -> RuntimeStyles.StaticRules rnRulesObject

let private processIntoRuntimeOptimizedStructure(blocks: List<ISheetBuildingBlock>) : RuntimeStyles =
    match blocks with
    | [] ->
        Log.Error "For style sheets, do not pass empty list to `compile`, use `RuntimeStyles.None` instead"
        RuntimeStyles.None
    | _ ->
        blocks
        |> flattenBuildingBlocks
        |> processPairs

let makeSheet (blocks: List<ISheetBuildingBlock>) : Styles =
    blocks |> flattenBuildingBlocks |> Styles.Sheet

// short alias for .style files
let compile = processIntoRuntimeOptimizedStructure
let compileLazy styles = lazy processIntoRuntimeOptimizedStructure styles
let rules = flattenAndBuildRnInternalRepresentation

// sadly need this to help type inference along (see Button.styles.fs for example usage)
let asBlocks (blocks: List<ISheetBuildingBlock>) : List<ISheetBuildingBlock> = blocks

let ( ==> ) (selector: Selector) (styles: Styles) : SheetBuildingBlockOne =
    SheetBuildingBlockOne (selector, styles)

let ( => ) (selector: Selector) (rawValues: seq<RuleFunctionReturnedStyleRules>) : SheetBuildingBlockOne =
    selector ==> flattenAndBuildRnInternalRepresentation rawValues

let private prependSelectorOne (selectorToPrepend: Selector) (selector: Selector, styles: Styles) : SheetBuildingBlockOne =
    SheetBuildingBlockOne(selectorToPrepend + " && " + selector, styles)

let private prependSelector (selector: Selector) (block: ISheetBuildingBlock) : ISheetBuildingBlock =
    block.Match
        (fun one  -> prependSelectorOne selector one.ToTuple :> ISheetBuildingBlock)
        (fun many -> SheetBuildingBlockMany (many.Blocks |> List.map (fun inputTuple -> (prependSelectorOne selector inputTuple).ToTuple)) :> ISheetBuildingBlock)

let ( && ) (blockToExtend: SheetBuildingBlockOne) (blocks: List<ISheetBuildingBlock>) : ISheetBuildingBlock =
    let newBlocks: List<Selector * Styles> =
        blocks
        |> List.map (prependSelector blockToExtend.Selector)
        |> flattenBuildingBlocks

    (SheetBuildingBlockMany (blockToExtend.ToTuple :: newBlocks)) :> ISheetBuildingBlock
