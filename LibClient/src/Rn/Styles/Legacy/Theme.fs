namespace Rn.LegacyStyles

open Rn.LegacyStyles

[<AutoOpen>]
module Theme =
    let makeCustomize(componentName: string, baseStyles: Lazy<List<ISheetBuildingBlock>>) : List<List<ISheetBuildingBlock>> -> unit =
        fun (additionalStyles: List<List<ISheetBuildingBlock>>) ->
            let styles = List.concat (baseStyles.Value :: additionalStyles)
            LibClient.ComponentRegistry.RegisterStyles (componentName, lazy (compile styles))
