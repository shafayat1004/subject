[<AutoOpen>]
module AppEggShellGallery.Components.Route_Components

open Fable.React
open LibClient
open LibClient.Components
open LibClient.Responsive
open LibRouter.Components
open ThirdParty.Showdown.Components
open ThirdParty.Showdown.Components.Constructors
open AppEggShellGallery.AppServices
open AppEggShellGallery.Navigation
open AppEggShellGallery.RenderHelpers
open AppEggShellGallery.SampleVisualsScreenSize

let private renderContent (content: ComponentItem) : ReactElement =
    match content with
    | Index ->
        Showdown.MarkdownViewer(
            source = MarkdownViewer.Url ("/docs/components/index.md" |> services().Http.PrepareInBundleResourceUrl),
            globalLinkHandler = "globalMarkdownLinkHandler",
            showdownConverter = showdownConverterWithSyntaxHighlighting
        )
    | Avatar                      -> Ui.Content.Avatar()
    | AsyncData                     -> Ui.Content.AsyncData()
    | ComponentItem.Button          -> Ui.Content.Button()
    | ComponentItem.Buttons         -> Ui.Content.Buttons()
    | Card                          -> Ui.Content.Card()
    | Carousel                      -> Ui.Content.Carousel()
    | ComponentItem.ContextMenu     -> Ui.Content.ContextMenu()
    | DateSelector                  -> Ui.Content.DateSelector()
    | Dialogs                       -> Ui.Content.Dialogs()
    | Draggable                     -> Ui.Content.Draggable()
    | ErrorBoundary                 -> Ui.Content.ErrorBoundary()
    | FloatingActionButton          -> Ui.Content.FloatingActionButton()
    | Forms                         -> Ui.Content.Forms()
    | AutoUi_InputForm              -> Ui.Content.AutoUi_InputForm()
    | Grid                          -> Ui.Content.Grid()
    | ComponentItem.Heading           -> Ui.Content.Heading()
    | Icon                          -> Ui.Content.Icon()
    | IconButton                    -> Ui.Content.IconButton()
    | IconWithBadge                 -> Ui.Content.IconWithBadge()
    | ImageCard                     -> Ui.Content.ImageCard()
    | InfoMessage                   -> Ui.Content.InfoMessage()
    | Input_Checkbox                -> Ui.Content.Input.Checkbox()
    | Input_ChoiceList              -> Ui.Content.Input.ChoiceList()
    | Input_Date                    -> Ui.Content.Input.Date()
    | Input_DayOfTheWeek            -> Ui.Content.Input.DayOfTheWeek()
    | Input_Decimal                 -> Ui.Content.Input.Decimal()
    | Input_Duration                -> Ui.Content.Input.Duration()
    | Input_EmailAddress            -> Ui.Content.Input.EmailAddress()
    | Input_LocalTime               -> Ui.Content.Input.LocalTime()
    | Input_File                    -> Ui.Content.Input.File()
    | Input_Image                   -> Ui.Content.Input.Image()
    | Input_Quantity                -> Ui.Content.Input.Quantity()
    | Input_PhoneNumber             -> Ui.Content.Input.PhoneNumber()
    | Input_PositiveInteger         -> Ui.Content.Input.PositiveInteger()
    | Input_PositiveDecimal         -> Ui.Content.Input.PositiveDecimal()
    | Input_UnsignedInteger         -> Ui.Content.Input.UnsignedInteger()
    | Input_UnsignedDecimal         -> Ui.Content.Input.UnsignedDecimal()
    | Input_Text                    -> Ui.Content.Input.Text()
    | ItemList                      -> Ui.Content.ItemList()
    | LabelledFormField             -> Ui.Content.LabelledFormField()
    | Input_Picker                  -> Ui.Content.Input.Picker()
    | InProgress                    -> Ui.XmlDocsContent.LC.InProgress()
    | Layout_Row                    -> Ui.XmlDocsContent.LC.Row()
    | Layout_Column                 -> Ui.XmlDocsContent.LC.Column()
    | Layout_Sized                  -> Ui.XmlDocsContent.LC.Sized()
    | Layout_Constrained            -> Ui.XmlDocsContent.LC.Constrained()
    | Nav_Top                       -> Ui.Content.Nav.Top()
    | Nav_Bottom                    -> Ui.Content.Nav.Bottom()
    | Pre                           -> Ui.Content.Pre()
    | QueryGrid                     -> Ui.Content.QueryGrid()
    | Section_Padded                -> Ui.Content.Section.Padded()
    | Stars                         -> Ui.Content.Stars()
    | Scrim                         -> Ui.Content.Scrim()
    | Sidebar                       -> Ui.Content.Sidebar()
    | Tabs                          -> Ui.Content.Tabs()
    | Tag                           -> Ui.Content.Tag()
    | TextButton                    -> Ui.Content.TextButton()
    | TimeSpan                      -> Ui.Content.TimeSpan()
    | Timestamp                     -> Ui.Content.Timestamp()
    | ThirdParty_Map                -> Ui.Content.ThirdParty.Map()
    | ThirdParty_MarkdownViewer     -> Ui.Content.ThirdParty.MarkdownViewer()
    | ThirdParty_ImagePicker        -> Ui.Content.ThirdParty.ImagePicker()
    | ThirdParty_ReCaptcha          -> Ui.Content.ThirdParty.ReCaptcha()
    | ThirdParty_Recharts           -> Ui.Content.ThirdParty.Recharts()
    | Thumb                         -> Ui.Content.Thumb()
    | Thumbs                        -> Ui.Content.Thumbs()
    | ToggleButtons                 -> Ui.Content.ToggleButtons()
    | SegmentedControl              -> Ui.Content.SegmentedControl()
    | Accessibility_Group           -> Ui.Content.Accessibility.Group()
    | Accessibility_LiveRegion      -> Ui.Content.Accessibility.LiveRegion()
    | Accessibility_WithAccessibility -> Ui.Content.Accessibility.WithAccessibility()
    | WithDataFlowControl           -> Ui.Content.With.DataFlowControl()
    | WithExecutor                  -> Ui.XmlDocsContent.LC.With_Executor()
    | WithSortAndFilter             -> Ui.Content.WithSortAndFilter()
    | Executor_AlertErrors          -> Ui.Content.Executor.AlertErrors()
    | AnimatableImage               -> Ui.Content.AnimatableImage()
    | AnimatableText                -> Ui.Content.AnimatableText()
    | AnimatableTextInput           -> Ui.Content.AnimatableTextInput()
    | AnimatableView                -> Ui.Content.AnimatableView()
    | TouchableOpacity              -> Ui.Content.TouchableOpacity()
    | LibRouter_Dialogs             -> Ui.Content.LibRouter.Dialogs()
    | LibRouter_LogRouteTransitions -> Ui.Content.LibRouter.LogRouteTransitions()
    | LibRouter_NativeBackButton    -> Ui.Content.LibRouter.NativeBackButton()
    | LibRouter_WithLocation        -> Ui.Content.LibRouter.WithLocation()
    | LibRouter_WithRoute           -> Ui.Content.LibRouter.WithRoute()
    | _                             -> LC.Text "Docs not available yet — why don't you add it?"

type Ui.Route with
    [<Component>]
    static member Components(pstoreKey: string, sampleVisualsScreenSize: ScreenSize, content: ComponentItem) : ReactElement =
        sampleVisualsScreenSizeContextProvider sampleVisualsScreenSize [|
            element {
                LC.SetPageMetadata(title = ComponentItem.pageTitle content)
                LR.Route(
                    scroll = LibRouter.Components.Route.Vertical,
                    children = [|
                        LC.Section.Padded(
                            children = [| renderContent content |]
                        )
                    |]
                )
            }
        |]
