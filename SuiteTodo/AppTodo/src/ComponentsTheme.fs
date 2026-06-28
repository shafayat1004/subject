module AppTodo.ComponentsTheme

open AppTodo.Colors
open LibClient
open LibClient.Components

let applyTheme () : unit =
    LibClient.DefaultComponentsTheme.ApplyTheme.primarySecondary colors
    LibRouter.DefaultComponentsTheme.ApplyTheme.primarySecondary colors
