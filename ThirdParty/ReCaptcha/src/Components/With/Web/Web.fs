[<AutoOpen>]
module ThirdParty.ReCaptcha.Components.With.Web

open Fable.React
open LibClient
open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS
open ThirdParty.ReCaptcha.Components.Constructors

type ReCaptchaInstance =
    abstract execute: string -> Promise<string>

#if EGGSHELL_PLATFORM_IS_WEB
[<Fable.Core.JS.Pojo>]
type private ReCaptchaLoadOptionsJs ( autoHideBadge: bool ) =
    member val autoHideBadge = autoHideBadge

let private ReCaptchaLoader (_: string) (_: obj): Promise<ReCaptchaInstance> = import "load" "recaptcha-v3"

let private load (siteKey: string) : Async<ReCaptchaInstance> =
    async {
        return! ReCaptchaLoader siteKey (ReCaptchaLoadOptionsJs(true) |> box) |> Async.AwaitPromise
    }

let private execute (siteKey: string) (action: string) =
    async {
        let! reCaptcha = load siteKey
        return! reCaptcha.execute action |> Async.AwaitPromise
    }
#endif

type ThirdParty.ReCaptcha.Components.Constructors.ReCaptcha.With with
    [<Component>]
    static member Web(
            ``with``: (string -> Async<Result<NonemptyString, exn>>) -> ReactElement,
            siteKey:  string,
            ?key:     string
        ) : ReactElement =
        ignore key

        #if EGGSHELL_PLATFORM_IS_WEB
        Hooks.useEffect(
            (fun () -> load siteKey |> Async.Ignore |> startSafely),
            [| box siteKey |]
        )

        let getToken (action: string): Async<Result<NonemptyString, exn>> =
            async {
                try
                    let! token = execute siteKey action
                    return
                        match NonemptyString.ofString token with
                        | Some nonemptyTokenString -> Ok nonemptyTokenString
                        | None -> Error (failwith "empty token")
                with e ->
                    return Error e
            }

        ``with`` getToken
        #else
        failwith "ReCaptcha Web component should not be used on native"
        #endif
