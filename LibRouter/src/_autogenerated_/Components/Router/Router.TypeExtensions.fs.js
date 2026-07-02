
import { map, orElse, defaultArg } from "../../../../../AppEggShellGallery/src/fable_modules/fable-library-js.5.4.0/Option.js";
import { Make, Props, defaultFuture } from "../../../Components/Router/Router.typext.fs.js";
import { isEmpty } from "../../../../../AppEggShellGallery/src/fable_modules/fable-library-js.5.4.0/List.js";
import { Option_getOrElse } from "../../../../../LibLangFsharp/src/OptionExtensions.fs.js";
import { tellReactArrayKeysAreOkay } from "../../../../../LibClient/src/EggShellReact.fs.js";

export function LibRouter_Components_Constructors_LR__LR_Router_Static_Z2B405B8(children, future, key, initialEntries, xLegacyStyles) {
    let __props;
    const future_1 = defaultArg(future, defaultFuture);
    __props = (new Props(orElse(key, undefined), orElse(initialEntries, undefined), future_1));
    let matchResult, styles;
    if (xLegacyStyles != null) {
        if (isEmpty(xLegacyStyles)) {
            matchResult = 0;
        }
        else {
            matchResult = 1;
            styles = xLegacyStyles;
        }
    }
    else {
        matchResult = 0;
    }
    switch (matchResult) {
        case 1: {
            __props.__style = styles;
            break;
        }
    }
    return Make(__props)(Option_getOrElse([], map(tellReactArrayKeysAreOkay, children)));
}

