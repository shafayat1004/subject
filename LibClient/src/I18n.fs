/// Portable i18n runtime (Egg.Shell LibUiChaldal.I18n / scaffold convention).
module LibClient.I18n

open Fable.React
open LibClient.Services.Subscription

[<RequireQualifiedAccess>]
type Language =
| En
| Bn

type I18n<'T>(en: 'T, bn: 'T, subscribeOnChanges: (Language -> unit) -> SubscribeResult) =
    let mutable maybeSettings: Option<Language * 'T> = None

    let stringsForLanguage (language: Language) : 'T =
        match language with
        | Language.En -> en
        | Language.Bn -> bn

    let setLanguage (language: Language) : unit =
        maybeSettings <- Some (language, stringsForLanguage language)

    let setLanguageAndReloadApp (getApp: unit -> ReactElement) (language: Language) : unit =
        setLanguage language
        restartApp (getApp ()) ()

    let settings () : Language * 'T =
        match maybeSettings with
        | Some t -> t
        | None   -> failwith "I18n not initialized — call StartWithDefault in Bootstrap.fs before rendering."

    member _.CurrentLanguage : Language =
        settings () |> fst

    member _.StartWithDefault (getApp: unit -> ReactElement, language: Language) : unit =
        setLanguage language

        subscribeOnChanges (fun maybeUpdatedLanguage ->
            match maybeSettings with
            | Some (currentLanguage, _) when currentLanguage = maybeUpdatedLanguage -> Noop
            | _                                                                     -> setLanguageAndReloadApp getApp maybeUpdatedLanguage
        )
        |> ignore

    member _.t : 'T =
        settings () |> snd

    member _.Format (format: string, ?arg0: obj) : string =
        System.String.Format(format, arg0)

    member _.Format (format: string, ?arg0: obj, ?arg1: obj) : string =
        System.String.Format(format, arg0, arg1)

    member _.Format (format: string, ?arg0: obj, ?arg1: obj, ?arg2: obj) : string =
        System.String.Format(format, arg0, arg1, arg2)
