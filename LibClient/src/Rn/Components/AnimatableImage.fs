[<AutoOpen>]
module Rn.Components.AnimatableImage

open Rn.Helpers
open Rn.Styles
open Rn.Types

open Fable.Core.JsInterop
open Fable.Core
open Browser.Types

module Rn =
    module AnimatableImage =
        [<StringEnum>]
        type ResizeMethod =
        | Auto
        | Resize
        | Scale

        type ResizeMode = Rn.Components.Image.ResizeMode
        let Stretch = Rn.Components.Image.ResizeMode.Stretch
        let Contain = Rn.Components.Image.ResizeMode.Contain
        let Cover   = Rn.Components.Image.ResizeMode.Cover
        let Auto    = Rn.Components.Image.ResizeMode.Auto
        let Repeat  = Rn.Components.Image.ResizeMode.Repeat

        type Dimensions = {
            width:  int
            height: int
        }

open Rn.AnimatableImage

type Rn.Components.Constructors.Rn with
    static member AnimatableImage(
        source:              string,
        ?headers:            Headers,
        ?accessibilityLabel: string,
        ?resizeMode:         ResizeMode,
        ?resizeMethod:       ResizeMethod,
        ?title:              string,
        ?onLoad:             Dimensions -> unit,
        ?onError:            ErrorEvent -> unit,
        ?styles:             array<AnimatableViewStyles>
    ) =
        let __props = createEmpty

        __props?source             <- source
        __props?headers            <- headers
        __props?accessibilityLabel <- accessibilityLabel
        __props?resizeMode         <- resizeMode
        __props?resizeMethod       <- resizeMethod
        __props?title              <- title
        __props?onLoad             <- onLoad
        __props?onError            <- onError
        __props?style              <- styles

        Fable.React.ReactBindings.React.createElement(
            Rn.RnPrimitives.Animated?Image,
            __props,
            [||]
        )
