[<AutoOpen>]
module ReactXP.Components.ScrollView


open ReactXP.Helpers
open ReactXP.Types

open Fable.Core.JsInterop
open Browser.Types
open LibClient
open LibClient.JsInterop

type OverScrollMode =
| Auto
| Always
| Never

type TabNavigation =
| Local
| Cycle
| Once

type KeyboardDismissMode =
| [<CompiledName("none")>]KeyboardDismissMode
| Interactive
| [<CompiledName("on-drag")>]OnDrag

type ScrollIndicatorInsets = {
    top:    int
    left:   int
    bottom: int
    right:  int
}

// NOTE animation duration is rather stupidly hardcoded to 200 ms in the ReactXP source
type IScrollViewRef =
    abstract member setScrollLeft: (* scrollLeft *) int * (* animate *) bool -> unit;
    abstract member setScrollTop:  (* scrollTop *)  int * (* animate *) bool -> unit;

module private ScrollViewRN =
    let unboxStyles (styles: array<ReactXP.Styles.FSharpDialect.ScrollViewStyles> option) : array<obj> option =
        styles |> Option.map (Array.map (fun s -> (!!s) :> obj))

    // RN onScroll event: {nativeEvent: {contentOffset: {x, y}}}
    // ReactXP signature: int * int -> unit
    let wrapOnScroll (f: (int * int -> unit) option) : obj option =
        f |> Option.map (fun handler ->
            box (fun (e: obj) ->
                let x = e?nativeEvent?contentOffset?x |> int
                let y = e?nativeEvent?contentOffset?y |> int
                handler (x, y)))

    // ReactXP used bool; RN uses 'always'|'handled'|'never'
    let mapKeyboardShouldPersistTaps (v: bool option) : obj option =
        v |> Option.map (fun b -> if b then box "always" else box "never")

type ReactXP.Components.Constructors.RX with
    static member ScrollView(
        ?children:                       ReactChildrenProp,
        ?vertical:                       bool,
        ?horizontal:                     bool,
        ?onLayout:                       ViewOnLayoutEvent -> unit,
        ?onContentSizeChange:            int -> int -> unit,
        ?onScroll:                       int * int -> unit,
        ?onScrollBeginDrag:              unit -> unit,
        ?onScrollEndDrag:                unit -> unit,
        ?onKeyPress:                     KeyboardEvent -> unit,
        ?onFocus:                        FocusEvent -> unit,
        ?onBlur:                         FocusEvent -> unit,
        ?showsHorizontalScrollIndicator: bool,
        // showsVerticalScrollIndicator prop doesn't work for Web Apps. They have an open issue on this
        // https://github.com/microsoft/reactxp/issues/990
        ?showsVerticalScrollIndicator:   bool,
        ?scrollEnabled:                  bool,
        ?keyboardDismissMode:            KeyboardDismissMode,
        // keyboardShouldPersistTaps prop default value is set to true so that taps work even if the keyboard is visible
        ?keyboardShouldPersistTaps:      bool,
        ?scrollEventThrottle:            float,
        ?bounces:                        bool,
        ?pagingEnabled:                  bool,
        ?snapToInterval:                 float,
        ?scrollsToTop:                   bool,
        ?overScrollMode:                 OverScrollMode,
        ?scrollIndicatorInsets:          ScrollIndicatorInsets,
        ?tabNavigation:                  TabNavigation,
        ?ref:                            LibClient.JsInterop.JsNullable<IScrollViewRef> -> unit,
        ?testId:                         string,
        ?styles:                         array<ReactXP.Styles.FSharpDialect.ScrollViewStyles>,
        ?xLegacyStyles:                  List<ReactXP.LegacyStyles.RuntimeStyles>
        // need to add these when we deal with animations
        // scrollXAnimatedValue:            RX.Types.AnimatedValue      option // defaultWithAutoWrap Undefined
        // scrollYAnimatedValue:            RX.Types.AnimatedValue      option // defaultWithAutoWrap Undefined
    ) =
        // tabNavigation is ReactXP web-only; no RN equivalent
        ignore tabNavigation

        let __props = createEmpty

        __props?vertical                       <- vertical
        __props?horizontal                     <- horizontal
        __props?onLayout                       <- onLayout
        __props?onContentSizeChange            <- onContentSizeChange
        __props?onScroll                       <- ScrollViewRN.wrapOnScroll onScroll
        __props?onScrollBeginDrag              <- onScrollBeginDrag
        __props?onScrollEndDrag                <- onScrollEndDrag
        __props?onKeyPress                     <- onKeyPress
        __props?onFocus                        <- onFocus
        __props?onBlur                         <- onBlur
        __props?showsHorizontalScrollIndicator <- showsHorizontalScrollIndicator
        __props?showsVerticalScrollIndicator   <- showsVerticalScrollIndicator
        __props?scrollEnabled                  <- scrollEnabled
        __props?keyboardDismissMode            <- keyboardDismissMode
        __props?keyboardShouldPersistTaps      <-
            ScrollViewRN.mapKeyboardShouldPersistTaps (keyboardShouldPersistTaps |> Option.orElse (Some true))
        __props?scrollEventThrottle            <- scrollEventThrottle
        __props?bounces                        <- bounces
        __props?pagingEnabled                  <- pagingEnabled
        __props?snapToInterval                 <- snapToInterval
        __props?scrollsToTop                   <- scrollsToTop
        __props?overScrollMode                 <- overScrollMode
        __props?scrollIndicatorInsets          <- scrollIndicatorInsets
        __props?ref                            <- ref
        __props?style                          <- ScrollViewRN.unboxStyles styles

        ReactXP.RNSeam.assignTestId __props testId

        match xLegacyStyles with
        | Option.None | Option.Some [] -> ()
        | Option.Some ls -> __props?__style <- ls

        ReactXP.RNSeam.createElement
            ReactXP.RNSeam.ScrollView
            __props
            (ThirdParty.fixPotentiallySingleChild (Option.map tellReactArrayKeysAreOkay children |> Option.getOrElse [||]))
