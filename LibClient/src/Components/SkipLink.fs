[<AutoOpen>]
module LibClient.Components.SkipLink

open Fable.React
open LibClient

#if EGGSHELL_PLATFORM_IS_WEB
open Fable.Core.JsInterop
open Fable.React.ReactBindings
#endif

type LibClient.Components.Constructors.LC with
    /// Render a visually-hidden anchor that becomes visible on keyboard focus.
    /// Place as the first child of the app shell so keyboard users can skip to main content.
    /// On native this renders nothing.
    static member SkipLink(?targetId: string, ?label: string) : ReactElement =
#if EGGSHELL_PLATFORM_IS_WEB
        let href = "#" + (defaultArg targetId "eggshell-app-content")
        let labelText = defaultArg label "Skip to main content"
        let props = createEmpty
        props?href <- href
        props?className <- "eggshell-skip-link"
        React.createElement("a", props, [| !!labelText |])
#else
        noElement
#endif
