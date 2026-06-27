[<AutoOpen>]
module AppEggShellGallery.Components.Content_AsyncData

open Fable.React
open LibClient
open LibClient.Components
open AppEggShellGallery.Components
open AppEggShellGallery.Components.ComponentSample

type private Demos =
    [<Component>]
    static member WhenAvailableText (name: string) : ReactElement =
        LC.Text $"The name is {name}."

    [<Component>]
    static member ErrorBoundaryDemo () : ReactElement =
        let showFailure = Hooks.useState false

        LC.ErrorBoundary(
            ``try`` =
                element {
                    LC.Button(
                        label = "Trigger AsyncData failure",
                        state = Button.PropStateFactory.MakeLowLevel (Button.Actionable (fun _ -> showFailure.update true))
                    )
                    if showFailure.current then
                        LC.AsyncData(
                            data = Failed (UserReadableFailure "someone sent us the bomb"),
                            whenAvailable = Demos.WhenAvailableText
                        )
                },
            catch =
                (fun (_, _) ->
                    LC.Text "Caught an error (catching here lest the error propagates all the way to the top of the app)"
                )
        )

type Ui.Content with
    [<Component>]
    static member AsyncData () : ReactElement =
        Ui.ComponentContent(
            displayName = "AsyncData",
            props = ComponentContent.ForFullyQualifiedName "LibClient.Components.AsyncData",
            notes =
                LC.Text
                    "An AsyncData component is typically used in conjunction with a component that provides data asynchronously, such as With.Subject or QueryGrid. Such components will typically handle the async life cycle on your behalf, automatically transitioning between different AsyncData<'T> values. For the sake of simplicity, the examples below provide AsyncData<'T> values to the AsyncData component directly.",
            samples =
                element {
                    LC.Fragment(
                        key = "basics",
                        children =
                            [|
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Uninitialized,
                                            whenAvailable = Demos.WhenAvailableText
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Uninitialized,
    whenAvailable = fun name -> LC.Text $"The name is {name}."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Unavailable,
                                            whenAvailable = Demos.WhenAvailableText
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Unavailable,
    whenAvailable = fun name -> LC.Text $"The name is {name}."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = AccessDenied,
                                            whenAvailable = Demos.WhenAvailableText
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = AccessDenied,
    whenAvailable = fun name -> LC.Text $"The name is {name}."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = WillStartFetchingSoonHack,
                                            whenAvailable = Demos.WhenAvailableText
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = WillStartFetchingSoonHack,
    whenAvailable = fun name -> LC.Text $"The name is {name}."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Fetching None,
                                            whenAvailable = Demos.WhenAvailableText
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Fetching None,
    whenAvailable = fun name -> LC.Text $"The name is {name}."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Fetching (Some "Jekyll"),
                                            whenAvailable = Demos.WhenAvailableText
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Fetching (Some "Jekyll"),
    whenAvailable = fun name -> LC.Text $"The name is {name}."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Available "Hyde",
                                            whenAvailable = Demos.WhenAvailableText
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Available "Hyde",
    whenAvailable = fun name -> LC.Text $"The name is {name}."
)
"""
                                        )
                                )
                            |]
                    )

                    LC.Fragment(
                        key = "customization",
                        children =
                            [|
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Uninitialized,
                                            whenAvailable = Demos.WhenAvailableText,
                                            whenUninitialized = fun () -> LC.Text "A custom uninitialized message."
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Uninitialized,
    whenAvailable = fun name -> LC.Text $"The name is {name}.",
    whenUninitialized = fun () -> LC.Text "A custom uninitialized message."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Unavailable,
                                            whenAvailable = Demos.WhenAvailableText,
                                            whenElse = fun () -> LC.Text "An alternative way to customize for all states other than Available."
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Unavailable,
    whenAvailable = fun name -> LC.Text $"The name is {name}.",
    whenElse = fun () -> LC.Text "An alternative way to customize for all states other than Available."
)
"""
                                        )
                                )
                                Ui.ComponentSample(
                                    visuals =
                                        LC.AsyncData(
                                            data = Fetching (Some "Jekyll"),
                                            whenAvailable = Demos.WhenAvailableText,
                                            whenFetching =
                                                (fun maybePrevName ->
                                                    let prev = maybePrevName |> Option.getOrElse "unknown"
                                                    LC.Text ("Updating name (previously " + prev + "), please wait.")
                                                )
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Fetching (Some "Jekyll"),
    whenAvailable = fun name -> LC.Text $"The name is {name}.",
    whenFetching = fun maybePrevName ->
        let prev = maybePrevName |> Option.getOrElse "unknown"
        LC.Text ("Updating name (previously " + prev + "), please wait.")
)
"""
                                        )
                                )
                            |]
                    )

                    LC.Fragment(
                        key = "failures",
                        children =
                            [|
                                Ui.ComponentSample(
                                    heading = "With ErrorBoundary (triggered on demand)",
                                    visuals = Demos.ErrorBoundaryDemo(),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.ErrorBoundary(
    ``try`` = ...,
    catch = fun (_, _) -> ...
)"""
                                        )
                                )
                                Ui.ComponentSample(
                                    heading = "With WhenFailed handler",
                                    visuals =
                                        LC.AsyncData(
                                            data = Failed (UserReadableFailure "someone sent us a bomb"),
                                            whenAvailable = Demos.WhenAvailableText,
                                            whenFailed = fun _ -> LC.Text "Something went wrong - we couldn't retrieve the name."
                                        ),
                                    code =
                                        ComponentSample.singleBlock Fsharp (
                                            LC.Text """
LC.AsyncData(
    data = Failed (UserReadableFailure "..."),
    whenAvailable = fun name -> ...,
    whenFailed = fun _ -> LC.Text "Something went wrong - we couldn't retrieve the name."
)
"""
                                        )
                                )
                            |]
                    )
                }
        )
