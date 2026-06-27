namespace AppEggShellGallery.Components

open Fable.React
open ReactXP.LegacyStyles

// Backward-compatible render module — referenced by auto-generated ComponentRegistration.fs.
module CodeRender =

    type Props = unit
    type Estate = unit
    type Pstate = unit
    type Actions = unit

    let render
            (_children:        array<ReactElement>,
             _props:           Props,
             _estate:          Estate,
             _pstate:          Pstate,
             _actions:         Actions,
             _componentStyles: RuntimeStyles)
            : ReactElement =
        failwith "Code is a modern [<Component>] — this render path is never called"
