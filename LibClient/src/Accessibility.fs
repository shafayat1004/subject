/// Cross-platform accessibility types aligned with @chaldal/reactxp CommonAccessibilityProps.
module LibClient.Accessibility

open Fable.Core
open Fable.Core.JsInterop
type AccessibilityRole =
| Alert = 0
| Imagebutton = 1
| Keyboardkey = 2
| ProgressBar = 3
| Radio = 4
| RadioGroup = 5
| ScrollBar = 6
| SpinButton = 7
| Timer = 8
| ToggleButton = 9
| Toolbar = 10
| Summary = 11
| Adjustable = 12
| Button = 13
| Tab = 14
| Link = 15
| Header = 16
| Search = 17
| Image = 18
| Text = 19
| Menu = 20
| MenuItem = 21
| MenuBar = 22
| TabList = 23
| List = 24
| ListItem = 25
| ListBox = 26
| Group = 27
| CheckBox = 28
| Checked = 29
| ComboBox = 30
| Log = 31
| Status = 32
| Dialog = 33
| HasPopup = 34
| Option = 35
| Switch = 36
| None = 37

type ImportantForAccessibility =
| Auto = 1
| Yes = 2
| No = 3
| NoHideDescendants = 4

type AccessibilityLiveRegion =
| None = 0
| Polite = 1
| Assertive = 2

type AccessibilityStateRecord = {
    Disabled: bool option
    Selected: bool option
    Checked: bool option
    Expanded: bool option
    Busy: bool option
}

module AccessibilityStateRecord =
    let empty = {
        Disabled = None
        Selected = None
        Checked = None
        Expanded = None
        Busy = None
    }

    let disabled value = { empty with Disabled = Some value }
    let selected value = { empty with Selected = Some value }
    let checked' value = { empty with Checked = Some value }
    let expanded value = { empty with Expanded = Some value }
    let busy value = { empty with Busy = Some value }

    let toJs (state: AccessibilityStateRecord) : obj =
        createObj [
            yield! (state.Disabled |> Option.map (fun v -> ("disabled", box v)) |> Option.toList)
            yield! (state.Selected |> Option.map (fun v -> ("selected", box v)) |> Option.toList)
            yield! (state.Checked |> Option.map (fun v -> ("checked", box v)) |> Option.toList)
            yield! (state.Expanded |> Option.map (fun v -> ("expanded", box v)) |> Option.toList)
            yield! (state.Busy |> Option.map (fun v -> ("busy", box v)) |> Option.toList)
        ]

type A11yProps = {
    Label: string option
    Role: AccessibilityRole
    State: AccessibilityStateRecord
    TestId: string option
    AccessibilityId: string option
    ImportantForAccessibility: ImportantForAccessibility option
    LiveRegion: AccessibilityLiveRegion option
    TabIndex: int option
    Actions: string list
}

module A11yProps =
    let defaults = {
        Label = None
        Role = AccessibilityRole.Button
        State = AccessibilityStateRecord.empty
        TestId = None
        AccessibilityId = None
        ImportantForAccessibility = None
        LiveRegion = None
        TabIndex = None
        Actions = []
    }

module A11ySlug =
    let fromLabel (label: string) =
        label
            .ToLowerInvariant()
            .Replace(".", "-")
            .Replace(" ", "-")
            .Replace("/", "-")

    let testId prefix label = sprintf "%s-%s" prefix (fromLabel label)

/// Reactive OS accessibility flags (§6 / ACCESSIBILITY_PLAN backlog #7).
type AccessibilitySettings = {
    ScreenReaderEnabled: bool
    ReduceMotion: bool
    BoldText: bool
    ReduceTransparency: bool
    InvertColors: bool
    Grayscale: bool
    FontScale: float
}

module AccessibilitySettings =
    let defaults = {
        ScreenReaderEnabled = false
        ReduceMotion = false
        BoldText = false
        ReduceTransparency = false
        InvertColors = false
        Grayscale = false
        FontScale = 1.0
    }
