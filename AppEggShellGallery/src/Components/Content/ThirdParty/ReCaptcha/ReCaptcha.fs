[<AutoOpen>]
module AppEggShellGallery.Components.Content_ThirdParty_ReCaptcha

open Fable.React
open LibClient
open LibClient.Components
open ThirdParty.ReCaptcha.Components.Constructors
open ThirdParty.ReCaptcha.Components.With.Base

// Google reCAPTCHA test site key (always passes in development).
let private testSiteKey = "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MAxRCLwQ"

type private Helpers =
    [<Component>]
    static member Sample () : ReactElement =
        let tokenState = Hooks.useState None

        ThirdParty.ReCaptcha.Components.Constructors.ReCaptcha.With.Base(
            siteKey = testSiteKey,
            baseUrl = "https://localhost",
            ``with`` =
                (fun getToken ->
                    element {
                        LC.Button(
                            label = "Execute reCAPTCHA",
                            state =
                                Button.PropStateFactory.MakeLowLevel(
                                    Button.Actionable(
                                        fun _ ->
                                            async {
                                                let! result = getToken "gallery"
                                                tokenState.update (
                                                    Some(
                                                        result
                                                        |> Result.map (fun token -> token.Value)
                                                        |> Result.mapError (fun ex -> ex.Message)
                                                    )
                                                )
                                            }
                                            |> startSafely
                                    )
                                )
                        )

                        match tokenState.current with
                        | None -> LC.Text "Click the button to obtain a token."
                        | Some(Ok _) -> LC.Text "Token received."
                        | Some(Error message) -> LC.Text $"Error: {message}"
                    })
        )

type Ui.Content.ThirdParty with
    [<Component>]
    static member ReCaptcha () : ReactElement =
        Ui.ComponentContent(
            displayName = "ReCaptcha",
            props = ComponentContent.ForFullyQualifiedName "ThirdParty.ReCaptcha.Components.With.Base",
            notes =
                LC.Text
                    "ReCaptcha.With.Base loads reCAPTCHA v3 on web and a WebView bridge on native. Replace the site key with your own for production.",
            a11y =
                Ui.A11yPanel(
                    componentName = "ReCaptcha.With.Base",
                    role = "none (invisible verification provider)",
                    namePattern = "Trigger button label; reCAPTCHA badge is third-party",
                    stateNotes = "Invisible v3 challenge; exposes token via callback",
                    scalesWithFont = true,
                    contrastNotes = "Third-party reCAPTCHA badge styling not controlled by EggShell",
                    deferredTags = ["[third-party] reCAPTCHA badge"]
                ),
            samples =
                element {
                    Ui.ComponentSample(
                        visuals = Helpers.Sample(),
                        code =
                            ComponentSample.SingleBlock(
                                ComponentSample.Fsharp,
                                LC.Text """
ReCaptcha.With.Base(
    siteKey = Config.current().ReCaptchaSiteKey,
    baseUrl = Config.current().AppUrlBase,
    ``with`` = fun getToken ->
        LC.Button(
            label = "Submit",
            state = Button.Actionable (fun _ ->
                getToken "submit"
                |> Async.Map (Result.map ignore)
                |> Async.Ignore
                |> startSafely)
        )
)"""
                            )
                    )
                }
        )
