[<AutoOpen>]
module ThirdParty.ReCaptcha.Components.With.Base

open Fable.React
open LibClient
open LibClient.Components
open ThirdParty.ReCaptcha.Components.Constructors
open ThirdParty.ReCaptcha.Components.With.Web
open ThirdParty.ReCaptcha.Components.With.Native

type ThirdParty.ReCaptcha.Components.Constructors.ReCaptcha.With with
    [<Component>]
    static member Base(
            ``with``: (string -> Async<Result<NonemptyString, exn>>) -> ReactElement,
            siteKey:  string,
            baseUrl:  string,
            ?key:     string
        ) : ReactElement =
        ignore key

        #if EGGSHELL_PLATFORM_IS_WEB
        ignore baseUrl
        ReCaptcha.With.Web(``with`` = ``with``, siteKey = siteKey)
        #else
        ReCaptcha.With.Native(``with`` = ``with``, siteKey = siteKey, baseUrl = baseUrl)
        #endif
