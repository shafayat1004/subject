module AppTodo.Colors

open LibClient.ColorModule
open LibClient.ColorScheme

type AppTodoColorScheme() =
    inherit ColorSchemeWithDefaults()

    override this.Primary   : Variants = MaterialDesignColors.``Light Green``
    override this.Secondary : Variants = MaterialDesignColors.Cyan

let colors = AppTodoColorScheme()
