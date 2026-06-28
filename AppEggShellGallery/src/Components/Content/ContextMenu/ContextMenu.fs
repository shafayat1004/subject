[<AutoOpen>]
module AppEggShellGallery.Components.Content_ContextMenu

open Fable.React
open LibClient
open LibClient.Components
open LibClient.ContextMenus
open LibClient.Responsive

type private PolitenessLevel =
| Polite
| Impolite
| Mean

let private cart =
    {|
        ItemCount = 3
        IsEmpty   = false
    |}

let private politenessLevel = PolitenessLevel.Polite

let private menuItems =
    [
        Heading (sprintf "Shopping Cart (%i)" cart.ItemCount)
        match politenessLevel with
        | PolitenessLevel.Polite   -> ContextMenuItem.Button ("Continue shopping, please", ignore)
        | PolitenessLevel.Impolite -> ContextMenuItem.Button ("Continue shopping", ignore)
        | PolitenessLevel.Mean     -> ContextMenuItem.Button ("Buy more dammit!", ignore)
        ContextMenuItem.Button ("Save Cart", ignore)
        if not cart.IsEmpty then ContextMenuItem.Button ("Checkout", ignore)
        Divider
        ButtonCautionary ("Empty Cart", ignore)
    ]

type Ui.Content with
    [<Component>]
    static member ContextMenu () : ReactElement =
        Ui.ComponentContent(
            displayName = "Context Menu",
            isResponsive = true,
            notes =
                element {
                    LC.Text "We support buttons, cautionary buttons, dividers, and headings. Menu items expose testId context-menu-item-{slug} for automation."
                },
            samples =
                element {
                    Ui.ComponentSample(
                        visuals =
                            LC.Buttons(
                                children =
                                    elements {
                                        LC.Button(
                                            label = "Handheld Context Menu",
                                            state =
                                                ButtonHighLevelState.LowLevel(
                                                    ButtonLowLevelState.Actionable (fun e ->
                                                        ContextMenu.Open menuItems ScreenSize.Handheld e.MaybeSource NoopFn e)
                                                )
                                        )
                                    }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
let menuItems = [
    Heading (sprintf "Shopping Cart (%i)" cart.ItemCount)
    match politenessLevel with
    | Polite   -> ContextMenuItem.Button ("Continue shopping, please", ignore)
    | Impolite -> ContextMenuItem.Button ("Continue shopping",         ignore)
    | Mean     -> ContextMenuItem.Button ("Buy more dammit!",          ignore)
    ContextMenuItem.Button ("Save Cart", ignore)
    if not cart.IsEmpty then ContextMenuItem.Button ("Checkout", ignore)
    Divider
    ButtonCautionary ("Empty Cart", ignore)
]

LC.Button(
    label = "Handheld Context Menu",
    state = ButtonHighLevelState.LowLevel (
        ButtonLowLevelState.Actionable (fun e ->
            ContextMenu.Open menuItems ScreenSize.Handheld e.MaybeSource NoopFn e)
    )
)"""
                            )
                    )

                    Ui.ComponentSample(
                        visuals =
                            LC.Buttons(
                                children =
                                    elements {
                                        LC.Button(
                                            label = "Desktop Context Menu",
                                            state =
                                                ButtonHighLevelState.LowLevel(
                                                    ButtonLowLevelState.Actionable (fun e ->
                                                        ContextMenu.Open menuItems ScreenSize.Desktop e.MaybeSource NoopFn e)
                                                )
                                        )
                                    }
                            ),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
LC.Button(
    label = "Desktop Context Menu",
    state = ButtonHighLevelState.LowLevel (
        ButtonLowLevelState.Actionable (fun e ->
            ContextMenu.Open menuItems ScreenSize.Desktop e.MaybeSource NoopFn e)
    )
)"""
                            )
                    )
                }
        )
