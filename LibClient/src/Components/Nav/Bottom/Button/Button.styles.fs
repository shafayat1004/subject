module LibClient.Components.Nav.Bottom.ButtonStyles

open ReactXP.LegacyStyles

let private baseStyles = lazy (asBlocks [
    "button" => []
])

type (* class to enable named parameters *) Theme() =
    static member Customize = makeCustomize("LibClient.Components.Nav.Bottom.Button", baseStyles)

    static member BadgeColor (badgeColor: Color) : Styles =
        let badgeSheet =
            let blocks : List<ISheetBuildingBlock> =
                [
                    "badge" ==> LibClient.Components.BadgeStyles.Theme.One (14, RulesRestricted.FontWeight.Bold, Color.White, badgeColor)
                ]
            blocks |> makeSheet

        let blocks : List<ISheetBuildingBlock> =
            [
                "button" ==> badgeSheet
            ]
        blocks |> makeSheet
        
let styles = lazy (compile (List.concat [
    baseStyles.Value
]))
