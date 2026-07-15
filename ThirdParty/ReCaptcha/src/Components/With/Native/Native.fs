[<AutoOpen>]
module ThirdParty.ReCaptcha.Components.With.Native

open Fable.React
open LibClient
open LibClient.Components
open LibClient.JsInterop
open Rn.Components
open Rn.Components.WebView
open ThirdParty.ReCaptcha.Components.Constructors

type ThirdParty.ReCaptcha.Components.Constructors.ReCaptcha.With with
    [<Component>]
    static member Native(
            ``with``: (string -> Async<Result<NonemptyString, exn>>) -> ReactElement,
            siteKey:  string,
            baseUrl:  string,
            ?key:     string
        ) : ReactElement =
        ignore key

        let maybeBrowserRef = Hooks.useState None
        let deferredRef = Hooks.useRef (LibLangFsharp.Deferred<Result<NonemptyString, exn>>())

        let sourceHtml (siteKey: string) =
            sprintf "<!DOCTYPE html><html><head>
        <script src=\"https://www.google.com/recaptcha/api.js?render=%s\"></script>
        <script>

            function getRecaptchaToken(siteKey, action) {
                window.grecaptcha.ready(function() {
                    window.grecaptcha.execute(siteKey, { action: action }).then(
                        function(args) {
                            window.ReactNativeWebView.postMessage(args);
                        }
                    )
                })
            }

            document.addEventListener(\"message\",(event)=>{
                getRecaptchaToken(\"%s\", event.data)
            })
        </script>
        </head></html>" siteKey siteKey

        let getToken (action: string): Async<Result<NonemptyString, exn>> =
            maybeBrowserRef.current
            |> Option.sideEffect (fun webviewRef -> webviewRef.postMessage action)
            deferredRef.current.Value

        let onMessageHandler (e: WebViewMessageEvent) =
            if deferredRef.current.IsPending then
                match NonemptyString.ofString e.data with
                | Some nonemptyToken ->
                    Ok nonemptyToken
                    |> deferredRef.current.Resolve
                | None -> (failwith "empty token") |> deferredRef.current.Reject

        Rn.View(
            children =
                [|
                    Rn.WebView(
                        javaScriptEnabled = true,
                        ref =
                            (fun (nullableInstance: JsNullable<WebViewMethods>) ->
                                maybeBrowserRef.update nullableInstance.ToOption),
                        source    = { html = sourceHtml siteKey; baseUrl = Some baseUrl },
                        onMessage = onMessageHandler
                    )
                    ``with`` getToken
                |]
        )
