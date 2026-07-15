/// Injects web-only global CSS for :focus-visible rings and the skip-link.
/// Call injectIfNeeded () once at app startup (LC.AppShell.Content does this automatically).
/// On native this module is a no-op.
module LibClient.FocusVisibleStyles

#if EGGSHELL_PLATFORM_IS_WEB
open Fable.Core.JsInterop

let private css =
    """
.eggshell-skip-link {
  position: absolute;
  top: -9999px;
  left: 8px;
  z-index: 9999;
  padding: 8px 16px;
  background: #FFFFFF;
  border: 2px solid #005FCC;
  border-radius: 4px;
  color: #005FCC;
  text-decoration: none;
  font-size: 14px;
  font-weight: 600;
  white-space: nowrap;
  outline: none;
}
.eggshell-skip-link:focus-visible {
  top: 8px;
  outline: 2px solid #005FCC;
  outline-offset: 2px;
}
[role="button"]:focus-visible,
[role="tab"]:focus-visible,
[role="option"]:focus-visible,
[role="radio"]:focus-visible,
[role="checkbox"]:focus-visible,
[role="switch"]:focus-visible,
[role="link"]:focus-visible,
[role="menuitem"]:focus-visible,
[role="combobox"]:focus-visible {
  outline: 2px solid rgba(0, 95, 204, 0.8);
  outline-offset: 2px;
  border-radius: 2px;
}
[role="button"]:focus:not(:focus-visible),
[role="tab"]:focus:not(:focus-visible),
[role="option"]:focus:not(:focus-visible),
[role="radio"]:focus:not(:focus-visible),
[role="checkbox"]:focus:not(:focus-visible),
[role="switch"]:focus:not(:focus-visible),
[role="link"]:focus:not(:focus-visible),
[role="menuitem"]:focus:not(:focus-visible),
[role="combobox"]:focus:not(:focus-visible) {
  outline: none;
}
"""

let injectIfNeeded () : unit =
    let id = "eggshell-a11y-focus-styles"
    if Browser.Dom.document.getElementById id |> isNull then
        let style = Browser.Dom.document.createElement "style"
        style?id <- id
        style?textContent <- css
        Browser.Dom.document.head?appendChild style |> ignore
#else
let injectIfNeeded () : unit = ()
#endif
