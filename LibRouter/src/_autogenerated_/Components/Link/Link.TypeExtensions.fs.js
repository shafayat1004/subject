
import { map, orElse } from "../../../../../AppEggShellGallery/src/fable_modules/fable-library-js.5.4.0/Option.js";
import { Make, Props } from "../../../Components/Link/Link.typext.fs.js";
import { isEmpty } from "../../../../../AppEggShellGallery/src/fable_modules/fable-library-js.5.4.0/List.js";
import { Option_getOrElse } from "../../../../../LibLangFsharp/src/OptionExtensions.fs.js";
import { tellReactArrayKeysAreOkay } from "../../../../../LibClient/src/EggShellReact.fs.js";

export function LibRouter_Components_Constructors_LR__LR_Link_Static_349DC38(to, children, key, xLegacyStyles) {
    const __props = new Props(to, orElse(key, undefined));
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

