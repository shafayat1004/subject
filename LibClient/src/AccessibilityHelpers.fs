module LibClient.AccessibilityHelpers

open Fable.Core.JsInterop
open LibClient.Accessibility

let applyToProps (props: obj) (a11y: A11yProps) (disabled: bool) =
    a11y.Label |> Option.iter (fun v -> props?accessibilityLabel <- v)
    props?accessibilityRole <- a11y.Role
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

let roleName (role: AccessibilityRole) = string role
