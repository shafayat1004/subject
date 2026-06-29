// Sidebar nav item lists, factored out of the gallery Sidebar shell.
//
// The render DSL compiles a component's whole template into ONE `render` member.
// Sidebar's template carries ~250 nested Sidebar.Item/Divider/Heading elements
// (the "Components" route alone has ~130), and Fable's AST optimizer recurses
// per nested node, overflowing the ~1MB compiler worker-thread stack (exit 134).
// Each function here returns a single element built from a FLAT array literal
// (depth 1, not nested), so every member stays shallow and Fable compiles fine.
module AppEggShellGallery.Components.SidebarContent

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Components.Constructors
open AppEggShellGallery.Navigation

module SI = LibClient.Components.Sidebar.Item

let private icon = SI.Right.Icon AppEggShellGallery.Icons.Icon.EggShell

let private componentTestId (item: ComponentItem) =
    sprintf "sidebar-component-%s" (unionCaseName item)

let private compItem (label: string) (item: ComponentItem) (itemState: ComponentItem -> SI.State) =
    LC.Sidebar.Item(label = label, testId = componentTestId item, state = itemState item)

let private compItemIcon (label: string) (item: ComponentItem) (itemState: ComponentItem -> SI.State) =
    LC.Sidebar.Item(label = label, testId = componentTestId item, right = icon, state = itemState item)

let docsItems (itemState: string -> SI.State) : ReactElement =
    castAsElement [|
        LC.Sidebar.Item(label = "EggShell Introduction", state = itemState "index.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Item(label = "Getting Started", state = itemState "basics/getting-started.md")
        LC.Sidebar.Item(label = "Dev Experience",  state = itemState "basics/dev-experience.md")
        LC.Sidebar.Item(label = "Components",       state = itemState "fsharp/component.md")
        LC.Sidebar.Item(label = "Styling",          state = itemState "fsharp/styling.md")
        LC.Sidebar.Item(label = "Themeing",         state = itemState "fsharp/themeing.md")
        LC.Sidebar.Item(label = "Legacy Interop",   state = itemState "fsharp/legacy.md")
        LC.Sidebar.Item(label = "Libraries",        state = itemState "basics/libraries.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Native")
        LC.Sidebar.Item(label = "Getting Started",     state = itemState "native/getting-started.md")
        LC.Sidebar.Item(label = "Dev Experience",      state = itemState "native/dev-experience.md")
        LC.Sidebar.Item(label = "Link Native Libray",  state = itemState "native/link-native-library.md")
        LC.Sidebar.Item(label = "Release Native App",  state = itemState "native/release-app.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Housekeeping")
        LC.Sidebar.Item(label = "Changelog",              state = itemState "basics/changelog.md")
        LC.Sidebar.Item(label = "Roadmap",                state = itemState "basics/roadmap.md")
        LC.Sidebar.Item(label = "Where to find examples", state = itemState "fsharp/examples.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Unsorted")
        LC.Sidebar.Item(label = "Background",                                 state = itemState "unsorted/background.md")
        LC.Sidebar.Item(label = "Icons infra",                                state = itemState "unsorted/icons.md")
        LC.Sidebar.Item(label = "Component types",                            state = itemState "unsorted/component-design.md")
        LC.Sidebar.Item(label = "EggShell-specific F# Good Coding Practices", state = itemState "unsorted/eggshell-specific-fsharp-good-practices.md")
        LC.Sidebar.Item(label = "Directory structure",                        state = itemState "unsorted/directory-structure.md")
    |]

let toolsItems (itemState: string -> SI.State) : ReactElement =
    castAsElement [|
        LC.Sidebar.Item(label = "Tools Introduction", state = itemState "tools/index.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Item(label = "eggshell CLI", state = itemState "tools/cli.md")
        LC.Sidebar.Item(label = "Snippets",     state = itemState "tools/snippets.md")
    |]

let howToItems (itemStateMarkdown: string -> SI.State) : ReactElement =
    castAsElement [|
        LC.Sidebar.Item(label = "How To Introduction", state = itemStateMarkdown "how-to/index.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Item(label = "FAQ",                       state = itemStateMarkdown "how-to/faq.md")
        LC.Sidebar.Item(label = "Where to find examples",    state = itemStateMarkdown "how-to/projects.md")
        LC.Sidebar.Item(label = "Taps, Clicks, Hovers, etc", state = itemStateMarkdown "how-to/tap-capture.md")
        LC.Sidebar.Item(label = "Executors",                 state = itemStateMarkdown "how-to/executors.md")
        LC.Sidebar.Item(label = "Responsive Components",     state = itemStateMarkdown "how-to/responsive.md")
        LC.Sidebar.Item(label = "Scrolling in ReactXP",      state = itemStateMarkdown "how-to/scrolling.md")
        LC.Sidebar.Item(label = "React Refs",                state = itemStateMarkdown "how-to/refs.md")
        LC.Sidebar.Item(label = "Dealing with Spinners",     state = itemStateMarkdown "how-to/spinners.md")
    |]

let subjectItems (itemState: string -> SI.State) : ReactElement =
    castAsElement [|
        LC.Sidebar.Item(label = "Introduction", state = itemState "subject/index.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Item(label = "Actions and transitions",        state = itemState "subject/actions-and-transitions.md")
        LC.Sidebar.Item(label = "Events and subscriptions",       state = itemState "subject/events-and-subscriptions.md")
        LC.Sidebar.Item(label = "Construction and id generation", state = itemState "subject/construction-and-id-generation.md")
        LC.Sidebar.Item(label = "Indexing and querying",          state = itemState "subject/indexing-and-querying.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Item(label = "Testing",            state = itemState "subject/testing.md")
        LC.Sidebar.Item(label = "Dev Host Simulations", state = itemState "subject/dev-host-simulator.md")
        LC.Sidebar.Divider()
        LC.Sidebar.Item(label = "Views",          state = itemState "subject/views.md")
        LC.Sidebar.Item(label = "Access control", state = itemState "subject/access-control.md")
        LC.Sidebar.Item(label = "Consumption",    state = itemState "subject/consumption.md")
    |]

let designItems (itemState: DesignItem -> SI.State) : ReactElement =
    castAsElement [|
        LC.Sidebar.Item(label = "Design Introduction", state = itemState (DesignItem.Markdown "design/index.md"))
        LC.Sidebar.Divider()
        LC.Sidebar.Item(label = "Colors", state = itemState DesignItem.ColorVariants)
        LC.Sidebar.Item(label = "Icons",  state = itemState DesignItem.Icons)
    |]

let legacyItems (itemState: LegacyItem -> SI.State) : ReactElement =
    castAsElement [|
        LC.Sidebar.Item(label = "Legacy Introduction", state = itemState (LegacyItem.Markdown "design/index.md"))
        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Render DSL")
        LC.Sidebar.Item(label = "Language Description", state = itemState (LegacyItem.Markdown "renderDsl/index.md"))
        LC.Sidebar.Item(label = "Style Guide",          state = itemState (LegacyItem.Markdown "renderDsl/style-guide.md"))
        LC.Sidebar.Item(label = "Sunsetting",           state = itemState (LegacyItem.Markdown "fsharp/background.md"))
        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Styles DSL")
        LC.Sidebar.Item(label = "Language Description", state = itemState (LegacyItem.Markdown "stylesDsl/index.md"))
        LC.Sidebar.Item(label = "Style Guide",          state = itemState (LegacyItem.Markdown "stylesDsl/style-guide.md"))
    |]

let componentsItems (itemState: ComponentItem -> SI.State) : ReactElement =
    castAsElement [|
        compItem "Components Introduction" Index itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Layout")
        compItem "Row" Layout_Row itemState
        compItem "Column" Layout_Column itemState
        compItem "Sized" Layout_Sized itemState
        compItem "Constrained" Layout_Constrained itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Buttons")
        compItemIcon "Buttons" ComponentItem.Buttons itemState
        compItemIcon "Button" ComponentItem.Button itemState
        compItemIcon "IconButton" IconButton itemState
        compItemIcon "FloatingActionButton" FloatingActionButton itemState
        compItemIcon "TextButton" TextButton itemState
        compItemIcon "ToggleButtons" ToggleButtons itemState
        compItemIcon "SegmentedControl" SegmentedControl itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Input")
        compItemIcon "Forms" Forms itemState
        compItemIcon "AutoUi.InputForm" AutoUi_InputForm itemState
        compItemIcon "Input.Checkbox" Input_Checkbox itemState
        compItemIcon "Input.ChoiceList" Input_ChoiceList itemState
        compItemIcon "Input.Date" Input_Date itemState
        compItemIcon "Input.DayOfTheWeek" Input_DayOfTheWeek itemState
        compItemIcon "Input.Decimal" Input_Decimal itemState
        compItemIcon "Input.Duration" Input_Duration itemState
        compItemIcon "Input.EmailAddress" Input_EmailAddress itemState
        compItemIcon "Input.LocalTime" Input_LocalTime itemState
        compItemIcon "Input.File" Input_File itemState
        compItemIcon "Input.Image" Input_Image itemState
        compItemIcon "Input.Picker" Input_Picker itemState
        compItemIcon "Input.PhoneNumber" Input_PhoneNumber itemState
        compItemIcon "Input.PositiveInteger" Input_PositiveInteger itemState
        compItemIcon "Input.PositiveDecimal" Input_PositiveDecimal itemState
        compItemIcon "Input.Quantity" Input_Quantity itemState
        compItemIcon "Input.Text" Input_Text itemState
        compItemIcon "Input.UnsignedInteger" Input_UnsignedInteger itemState
        compItemIcon "Input.UnsignedDecimal" Input_UnsignedDecimal itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Content Blocks")
        compItemIcon "Card" Card itemState
        compItemIcon "Carousel" Carousel itemState
        compItemIcon "Dialogs" Dialogs itemState
        compItemIcon "Draggable" Draggable itemState
        compItemIcon "ImageCard" ImageCard itemState
        compItemIcon "InfoMessage" InfoMessage itemState
        compItemIcon "ItemList" ItemList itemState
        compItemIcon "Section.Padded" Section_Padded itemState
        compItemIcon "Tabs" Tabs itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Animation")
        compItemIcon "AnimatableImage" AnimatableImage itemState
        compItemIcon "AnimatableText" AnimatableText itemState
        compItemIcon "AnimatableTextInput" AnimatableTextInput itemState
        compItemIcon "AnimatableView" AnimatableView itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Admin Panels")
        compItemIcon "Grid" Grid itemState
        compItemIcon "QueryGrid" QueryGrid itemState
        LC.Sidebar.Item(label = "WithSortAndFilter", right = icon, state = SI.State.Disabled)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Text & Formatting")
        compItemIcon "Heading" ComponentItem.Heading itemState
        compItemIcon "Pre" Pre itemState
        compItemIcon "Tag" Tag itemState
        compItemIcon "TimeSpan" TimeSpan itemState
        compItemIcon "Timestamp" Timestamp itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Graphic")
        compItemIcon "Avatar" Avatar itemState
        compItemIcon "Icon" ComponentItem.Icon itemState
        compItemIcon "IconWithBadge" IconWithBadge itemState
        compItemIcon "Thumb" Thumb itemState
        compItemIcon "Thumbs" Thumbs itemState
        compItemIcon "Scrim" Scrim itemState
        compItemIcon "Stars" Stars itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Navigation")
        compItemIcon "Context Menu" ComponentItem.ContextMenu itemState
        compItemIcon "Sidebar" ComponentItem.Sidebar itemState
        compItemIcon "Nav.Top" Nav_Top itemState
        compItemIcon "Nav.Bottom" Nav_Bottom itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Higher Order Components")
        compItem "ErrorBoundary" ErrorBoundary itemState
        compItem "AlertErrors" Executor_AlertErrors itemState
        compItem "AsyncData" AsyncData itemState
        compItem "WithDataFlowControl" WithContext itemState
        compItem "TriStateful" TriStateful itemState
        compItem "QuadStateful" QuadStateful itemState
        compItem "Responsive" Responsive itemState
        compItem "InProgress" InProgress itemState
        compItem "With.Executor" WithExecutor itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Accessibility")
        compItem "Group" Accessibility_Group itemState
        compItem "LiveRegion" Accessibility_LiveRegion itemState
        compItem "With.Accessibility" Accessibility_WithAccessibility itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "With")
        compItem "WithContext" WithContext itemState
        compItem "DataFlowControl" WithDataFlowControl itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "LibRouter")
        compItem "LR.Dialogs" LibRouter_Dialogs itemState
        compItem "LR.LogRouteTransitions" LibRouter_LogRouteTransitions itemState
        compItem "LR.NativeBackButton" LibRouter_NativeBackButton itemState
        compItem "LR.With.Location" LibRouter_WithLocation itemState
        compItem "LR.With.Route" LibRouter_WithRoute itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Third Party")
        compItemIcon "MarkdownViewer" ThirdParty_MarkdownViewer itemState
        compItemIcon "Map" ThirdParty_Map itemState
        compItemIcon "ImagePicker" ThirdParty_ImagePicker itemState
        compItemIcon "ReCaptcha" ThirdParty_ReCaptcha itemState
        compItemIcon "Recharts" ThirdParty_Recharts itemState

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Unsorted")
        compItemIcon "DateSelector" DateSelector itemState
        compItemIcon "TouchableOpacity" TouchableOpacity itemState
        LC.Sidebar.Item(label = "Dialog.Confirm",                     right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.WhiteRounded.Base",     right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.WhiteRounded.Standard", right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.FullScren",             right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.FromBottom",            right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "DueDateTag",                         right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "FormFieldsDivider",                  right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "HandheldListItem",                   right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "HeaderCell",                         right = icon, state = SI.State.Disabled)
        compItemIcon "LabelledFormField" LabelledFormField itemState
        LC.Sidebar.Item(label = "LabelledValue",                      right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Popup",                              right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Route",                              right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "TwoWayScrollable",                   right = icon, state = SI.State.Disabled)
    |]
