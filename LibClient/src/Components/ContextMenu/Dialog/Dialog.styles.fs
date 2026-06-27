module LibClient.Components.ContextMenu.DialogStyles

open ReactXP.LegacyStyles

let styles = lazy (compile [
    "dialog-contents" => [
        Position.Absolute
        trbl 0 0 0 0
        FlexDirection.Column
        JustifyContent.FlexEnd
        padding 12
    ]

    "scroll-view" => [
        flex -1
    ]

    "divider" => [
        height 20
    ]

    "heading" => [
        color Color.White
        TextAlign.Center
    ]
])
