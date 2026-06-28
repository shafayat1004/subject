[<AutoOpen>]
module AppTodo.I18nGlobal

open AppTodo.AppServices
open LibClient
open LibClient.I18n

let private languageChangeQueue: LibClient.EventBus.Queue<Language> = LibClient.EventBus.Queue "languageChange"
let private localStorageKey = "language"

let setLanguage (language: Language) : unit =
    async {
        do! AppServices.services().LocalStorage.Put localStorageKey language Json.ToString<Language>
        LibClient.ServiceInstances.services().EventBus.Broadcast languageChangeQueue language
    } |> startSafely

let i18n =
    I18n(
        AppTodo.I18n.Languages.En.strings,
        AppTodo.I18n.Languages.Bn.strings,
        (fun (onMaybeChange: Language -> unit) ->
            let onResult = LibClient.ServiceInstances.services().EventBus.On languageChangeQueue onMaybeChange
            { Off = onResult.Off }
        )
    )

let start () =
    async {
        let! language = AppServices.services().LocalStorage.Get localStorageKey Json.FromString<Language>
        LibClient.ServiceInstances.services().EventBus.Broadcast languageChangeQueue (language |> Option.getOrElse Language.En)
    } |> startSafely
