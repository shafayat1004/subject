// Sidebar nav item lists, factored out of Sidebar.render.
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
        LC.Sidebar.Item(label = "Components Introduction", state = itemState Index)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Layout")
        LC.Sidebar.Item(label = "Row",         state = itemState Layout_Row)
        LC.Sidebar.Item(label = "Column",      state = itemState Layout_Column)
        LC.Sidebar.Item(label = "Sized",       state = itemState Layout_Sized)
        LC.Sidebar.Item(label = "Constrained", state = itemState Layout_Constrained)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Buttons")
        LC.Sidebar.Item(label = "Buttons",              right = icon, state = itemState ComponentItem.Buttons)
        LC.Sidebar.Item(label = "Button",               right = icon, state = itemState ComponentItem.Button)
        LC.Sidebar.Item(label = "IconButton",           right = icon, state = itemState IconButton)
        LC.Sidebar.Item(label = "FloatingActionButton", right = icon, state = itemState FloatingActionButton)
        LC.Sidebar.Item(label = "TextButton",           right = icon, state = itemState TextButton)
        LC.Sidebar.Item(label = "ToggleButtons",        right = icon, state = itemState ToggleButtons)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Input")
        LC.Sidebar.Item(label = "Forms",                 right = icon, state = itemState Forms)
        LC.Sidebar.Item(label = "Input.Checkbox",        right = icon, state = itemState Input_Checkbox)
        LC.Sidebar.Item(label = "Input.ChoiceList",      right = icon, state = itemState Input_ChoiceList)
        LC.Sidebar.Item(label = "Input.Date",            right = icon, state = itemState Input_Date)
        LC.Sidebar.Item(label = "Input.DayOfTheWeek",    right = icon, state = itemState Input_DayOfTheWeek)
        LC.Sidebar.Item(label = "Input.Decimal",         right = icon, state = itemState Input_Decimal)
        LC.Sidebar.Item(label = "Input.Duration",        right = icon, state = itemState Input_Duration)
        LC.Sidebar.Item(label = "Input.EmailAddress",    right = icon, state = itemState Input_EmailAddress)
        LC.Sidebar.Item(label = "Input.LocalTime",       right = icon, state = itemState Input_LocalTime)
        LC.Sidebar.Item(label = "Input.File",            right = icon, state = itemState Input_File)
        LC.Sidebar.Item(label = "Input.Image",           right = icon, state = itemState Input_Image)
        LC.Sidebar.Item(label = "Input.Picker",          right = icon, state = itemState Input_Picker)
        LC.Sidebar.Item(label = "Input.PhoneNumber",     right = icon, state = itemState Input_PhoneNumber)
        LC.Sidebar.Item(label = "Input.PositiveInteger", right = icon, state = itemState Input_PositiveInteger)
        LC.Sidebar.Item(label = "Input.PositiveDecimal", right = icon, state = itemState Input_PositiveDecimal)
        LC.Sidebar.Item(label = "Input.Quantity",        right = icon, state = itemState Input_Quantity)
        LC.Sidebar.Item(label = "Input.Text",            right = icon, state = itemState Input_Text)
        LC.Sidebar.Item(label = "Input.UnsignedInteger", right = icon, state = itemState Input_UnsignedInteger)
        LC.Sidebar.Item(label = "Input.UnsignedDecimal", right = icon, state = itemState Input_UnsignedDecimal)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Content Blocks")
        LC.Sidebar.Item(label = "Card",           right = icon, state = itemState Card)
        LC.Sidebar.Item(label = "Carousel",       right = icon, state = itemState Carousel)
        LC.Sidebar.Item(label = "Dialogs",        right = icon, state = itemState Dialogs)
        LC.Sidebar.Item(label = "Draggable",      right = icon, state = itemState Draggable)
        LC.Sidebar.Item(label = "ImageCard",      right = icon, state = itemState ImageCard)
        LC.Sidebar.Item(label = "InfoMessage",    right = icon, state = itemState InfoMessage)
        LC.Sidebar.Item(label = "ItemList",       right = icon, state = itemState ItemList)
        LC.Sidebar.Item(label = "Section.Padded", right = icon, state = itemState Section_Padded)
        LC.Sidebar.Item(label = "Tabs",           right = icon, state = itemState Tabs)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Animation")
        LC.Sidebar.Item(label = "AnimatableImage",     right = icon, state = itemState AnimatableImage)
        LC.Sidebar.Item(label = "AnimatableText",      right = icon, state = itemState AnimatableText)
        LC.Sidebar.Item(label = "AnimatableTextInput", right = icon, state = itemState AnimatableTextInput)
        LC.Sidebar.Item(label = "AnimatableView",      right = icon, state = itemState AnimatableView)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Admin Panels")
        LC.Sidebar.Item(label = "Grid",              right = icon, state = itemState Grid)
        LC.Sidebar.Item(label = "QueryGrid",         right = icon, state = itemState QueryGrid)
        LC.Sidebar.Item(label = "WithSortAndFilter", right = icon, state = SI.State.Disabled)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Text & Formatting")
        LC.Sidebar.Item(label = "Heading",   right = icon, state = itemState ComponentItem.Heading)
        LC.Sidebar.Item(label = "Pre",       right = icon, state = itemState Pre)
        LC.Sidebar.Item(label = "Tag",       right = icon, state = itemState Tag)
        LC.Sidebar.Item(label = "TimeSpan",  right = icon, state = itemState TimeSpan)
        LC.Sidebar.Item(label = "Timestamp", right = icon, state = itemState Timestamp)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Graphic")
        LC.Sidebar.Item(label = "Avatar",        right = icon, state = itemState Avatar)
        LC.Sidebar.Item(label = "Icon",          right = icon, state = itemState ComponentItem.Icon)
        LC.Sidebar.Item(label = "IconWithBadge", right = icon, state = itemState IconWithBadge)
        LC.Sidebar.Item(label = "Thumb",         right = icon, state = itemState Thumb)
        LC.Sidebar.Item(label = "Thumbs",        right = icon, state = itemState Thumbs)
        LC.Sidebar.Item(label = "Scrim",         right = icon, state = itemState Scrim)
        LC.Sidebar.Item(label = "Stars",         right = icon, state = itemState Stars)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Navigation")
        LC.Sidebar.Item(label = "Context Menu", right = icon, state = itemState ComponentItem.ContextMenu)
        LC.Sidebar.Item(label = "Sidebar",      right = icon, state = itemState ComponentItem.Sidebar)
        LC.Sidebar.Item(label = "Nav.Top",      right = icon, state = itemState Nav_Top)
        LC.Sidebar.Item(label = "Nav.Bottom",   right = icon, state = itemState Nav_Bottom)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Higher Order Components")
        LC.Sidebar.Item(label = "ErrorBoundary",       state = itemState ErrorBoundary)
        LC.Sidebar.Item(label = "AlertErrors",         state = itemState Executor_AlertErrors)
        LC.Sidebar.Item(label = "AsyncData",           state = itemState AsyncData)
        LC.Sidebar.Item(label = "WithDataFlowControl", state = itemState WithContext)
        LC.Sidebar.Item(label = "TriStateful",         state = itemState TriStateful)
        LC.Sidebar.Item(label = "QuadStateful",        state = itemState QuadStateful)
        LC.Sidebar.Item(label = "Responsive",          state = itemState Responsive)
        LC.Sidebar.Item(label = "InProgress",          state = itemState InProgress)
        LC.Sidebar.Item(label = "With.Executor",       state = itemState WithExecutor)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "With")
        LC.Sidebar.Item(label = "WithContext",     state = itemState WithContext)
        LC.Sidebar.Item(label = "DataFlowControl", state = itemState WithDataFlowControl)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Third Party")
        LC.Sidebar.Item(label = "Map",      right = icon, state = itemState ThirdParty_Map)
        LC.Sidebar.Item(label = "Recharts", right = icon, state = itemState ThirdParty_Recharts)

        LC.Sidebar.Divider()
        LC.Sidebar.Heading(text = "Unsorted")
        LC.Sidebar.Item(label = "DateSelector",                       right = icon, state = itemState DateSelector)
        LC.Sidebar.Item(label = "TouchableOpacity",                   right = icon, state = itemState TouchableOpacity)
        LC.Sidebar.Item(label = "Dialog.Confirm",                     right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.WhiteRounded.Base",     right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.WhiteRounded.Standard", right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.FullScren",             right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Dialog.Shell.FromBottom",            right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "DueDateTag",                         right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "FormFieldsDivider",                  right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "HandheldListItem",                   right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "HeaderCell",                         right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "LabelledFormField",                  right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "LabelledValue",                      right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Popup",                              right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "Route",                              right = icon, state = SI.State.Disabled)
        LC.Sidebar.Item(label = "TwoWayScrollable",                   right = icon, state = SI.State.Disabled)
    |]
