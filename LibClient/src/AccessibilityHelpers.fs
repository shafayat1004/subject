module LibClient.AccessibilityHelpers

open Fable.Core.JsInterop
open LibClient.Accessibility

let roleName (role: AccessibilityRole) = string role

/// Convert EggShell's integer AccessibilityRole enum to the string RN/RNW expects.
/// Returns None for roles RNW explicitly ignores.
let mapRoleToString (role: AccessibilityRole) : string option =
    match role with
    | AccessibilityRole.Alert         -> Some "alert"
    | AccessibilityRole.Imagebutton   -> None
    | AccessibilityRole.Keyboardkey   -> None
    | AccessibilityRole.ProgressBar   -> Some "progressbar"
    | AccessibilityRole.Radio         -> Some "radio"
    | AccessibilityRole.RadioGroup    -> Some "radiogroup"
    | AccessibilityRole.ScrollBar     -> Some "scrollbar"
    | AccessibilityRole.SpinButton    -> Some "spinbutton"
    | AccessibilityRole.Timer         -> Some "timer"
    | AccessibilityRole.ToggleButton  -> Some "button"
    | AccessibilityRole.Toolbar       -> Some "toolbar"
    | AccessibilityRole.Summary       -> Some "summary"
    | AccessibilityRole.Adjustable    -> Some "adjustable"
    | AccessibilityRole.Button        -> Some "button"
    | AccessibilityRole.Tab           -> Some "tab"
    | AccessibilityRole.Link          -> Some "link"
    | AccessibilityRole.Header        -> Some "banner"
    | AccessibilityRole.Search        -> Some "search"
    | AccessibilityRole.Image         -> Some "image"
    | AccessibilityRole.Text          -> None
    | AccessibilityRole.Menu          -> Some "menu"
    | AccessibilityRole.MenuItem      -> Some "menuitem"
    | AccessibilityRole.MenuBar       -> Some "menubar"
    | AccessibilityRole.TabList       -> Some "tablist"
    | AccessibilityRole.List          -> Some "list"
    | AccessibilityRole.ListItem      -> Some "listitem"
    | AccessibilityRole.ListBox       -> Some "listbox"
    | AccessibilityRole.Group         -> Some "group"
    | AccessibilityRole.CheckBox      -> Some "checkbox"
    | AccessibilityRole.Checked       -> Some "checkbox"
    | AccessibilityRole.ComboBox      -> Some "combobox"
    | AccessibilityRole.Log           -> Some "log"
    | AccessibilityRole.Status        -> Some "status"
    | AccessibilityRole.Dialog        -> Some "dialog"
    | AccessibilityRole.HasPopup      -> Some "button"
    | AccessibilityRole.Option        -> Some "option"
    | AccessibilityRole.Switch        -> Some "switch"
    | AccessibilityRole.None          -> Some "none"
    | AccessibilityRole.Main          -> Some "main"
    | AccessibilityRole.Navigation    -> Some "navigation"
    | AccessibilityRole.Complementary -> Some "complementary"
    | _                               -> None

let applyToProps (props: obj) (a11y: A11yProps) (disabled: bool) =
    a11y.Label |> Option.iter (fun v -> props?accessibilityLabel <- v)
    props?accessibilityRole <- (a11y.Role |> mapRoleToString |> Option.toObj)
    props?accessibilityState <- AccessibilityStateRecord.toJs a11y.State
    a11y.TestId |> Option.iter (fun v -> props?testId <- v)
    a11y.AccessibilityId |> Option.iter (fun v -> props?accessibilityId <- v)
    a11y.ImportantForAccessibility |> Option.iter (fun v -> props?importantForAccessibility <- v)
    a11y.LiveRegion |> Option.iter (fun v -> props?accessibilityLiveRegion <- v)
    a11y.TabIndex |> Option.iter (fun v -> props?tabIndex <- v)
    if not a11y.Actions.IsEmpty then
        props?accessibilityActions <- (a11y.Actions |> List.toArray)
    if disabled then
        props?disabled <- true
    a11y.Label |> Option.iter (fun v -> props?title <- v)
