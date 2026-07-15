[<AutoOpen>]
module AppEggShellGallery.Components.Content_Sidebar

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.Icons
open AppEggShellGallery.Colors
open AppEggShellGallery.LocalImages
open Rn.Components
open Rn.Styles

[<RequireQualifiedAccess>]
module private Styles =
    let sidebar = makeViewStyles { height 600 }
    let profile = makeViewStyles { padding 18 }
    let name    = makeTextStyles { FontWeight.Bold; fontSize 18; color (colors.Neutral.B600); marginTop 12 }
    let email   = makeTextStyles { fontSize 14; color (colors.Neutral.B500) }

module SI = LibClient.Components.Sidebar.Item

type Ui.Content with
    [<Component>]
    static member Sidebar () : ReactElement =
        Ui.ComponentContent (
            displayName = "Sidebar",
            props       = ComponentContent.Manual (element {
                Ui.ScrapedComponentProps (heading = "Sidebar.Base",    fullyQualifiedName = "LibClient.Components.Sidebar.Base")
                Ui.ScrapedComponentProps (heading = "Sidebar.Item",    fullyQualifiedName = "LibClient.Components.Sidebar.Item")
                Ui.ScrapedComponentProps (heading = "Sidebar.Heading", fullyQualifiedName = "LibClient.Components.Sidebar.Heading")
                Ui.ScrapedComponentProps (heading = "Sidebar.Divider", fullyQualifiedName = "LibClient.Components.Sidebar.Divider")
            }),
            a11y =
                Ui.A11yPanel(
                    componentName  = "LC.Sidebar.*",
                    role           = "navigation (Sidebar.Base); items expose button roles",
                    namePattern    = "Sidebar.Item label text; Sidebar.Heading for section headers",
                    stateNotes     = "Selected item exposes selected state; disabled items are not actionable",
                    scalesWithFont = true,
                    contrastNotes  = "Sidebar text and selection highlight meet WCAG AA"
                ),
            samples = (
                element {
                    Ui.ComponentSampleGroup(
                        samples = (
                            element {
                                Ui.ComponentSample(
                                    visuals = (
                                        LC.Sidebar.Base(
                                            styles   = [| Styles.sidebar |],
                                            fixedTop = Rn.View(
                                                styles   = [| Styles.profile |],
                                                children = [|
                                                    LC.Avatar(source                      = localImage "/images/tifa.jpg")
                                                    LC.UiText("Tifa Lockhart",    styles  = [| Styles.name  |])
                                                    LC.UiText("tifa@ffvii.world",  styles = [| Styles.email |])
                                                |]
                                            ),
                                            scrollableMiddle = castAsElement [|
                                                LC.Sidebar.Heading(text = "With Left Icons")
                                                LC.Sidebar.Item(label   = "Inbox",    leftIcon = Icon.TwoSheets, state = SI.State.Actionable ignore)
                                                LC.Sidebar.Item(label   = "Calendar", leftIcon = Icon.Calendar,  state = SI.State.Actionable ignore)
                                                LC.Sidebar.Item(label   = "Starred",  leftIcon = Icon.Star,      state = SI.State.Actionable ignore)
                                                LC.Sidebar.Item(label   = "Tags",     leftIcon = Icon.Tags,      state = SI.State.Actionable ignore)

                                                LC.Sidebar.Divider()
                                                LC.Sidebar.Heading(text = "Without Left Icons")
                                                LC.Sidebar.Item(label   = "Settings & Account", state = SI.State.Actionable ignore)
                                                LC.Sidebar.Item(label   = "Help & Feedback",    state = SI.State.Actionable ignore)

                                                LC.Sidebar.Divider()
                                                LC.Sidebar.Heading(text = "Right Icon/Badge")
                                                LC.Sidebar.Item(label   = "Notifications", right = SI.Right.Badge (PositiveInteger.ofLiteral 3), state = SI.State.Actionable ignore)
                                                LC.Sidebar.Item(label   = "Orders",        right = SI.Right.Icon Icon.Bell,                      state = SI.State.Actionable ignore)

                                                LC.Sidebar.Divider()
                                                LC.Sidebar.Heading(text = "States")
                                                LC.Sidebar.Item(label   = "Disabled",    state = SI.State.Disabled)
                                                LC.Sidebar.Item(label   = "Selected",    state = SI.State.Selected)
                                                LC.Sidebar.Item(label   = "In Progress", state = SI.State.InProgress)
                                            |],
                                            fixedBottom = LC.Sidebar.Item(label = "Log Out", leftIcon = Icon.Power, state = SI.State.Actionable ignore)
                                        )
                                    ),
                                    code = ComponentSample.SingleBlock (ComponentSample.Fsharp, LC.Text """
LC.Sidebar.Base(
    styles = [| makeViewStyles { height 600 } |],
    fixedTop = Rn.View(
        styles = [| makeViewStyles { padding 18 } |],
        children = [|
            LC.Avatar(source = localImage "/images/tifa.jpg")
            LC.UiText("Tifa Lockhart",    styles = [| makeTextStyles { FontWeight.Bold; fontSize 18 } |])
            LC.UiText("tifa@ffvii.world", styles = [| makeTextStyles { fontSize 14 } |])
        |]
    ),
    scrollableMiddle = castAsElement [|
        LC.Sidebar.Heading(text = "With Left Icons")
        LC.Sidebar.Item(label = "Inbox", leftIcon = Icon.TwoSheets, state = Sidebar.Item.State.Actionable ignore)
        LC.Sidebar.Item(label = "Calendar", leftIcon = Icon.Calendar, state = Sidebar.Item.State.Actionable ignore)
        ...
        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "States")
        LC.Sidebar.Item(label = "Selected", state = Sidebar.Item.State.Selected)
        LC.Sidebar.Item(label = "Disabled",  state = Sidebar.Item.State.Disabled)
    |],
    fixedBottom = LC.Sidebar.Item(label = "Log Out", leftIcon = Icon.Power, state = Sidebar.Item.State.Actionable ignore)
)""")
                                )
                            }
                        )
                    )
                }
            )
        )
