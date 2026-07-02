
import { map, orElse } from "../../../../../AppEggShellGallery/src/fable_modules/fable-library-js.5.4.0/Option.js";
import { Make, Props, NavigatorRoute } from "../../../Components/RXNavigator/RXNavigator.typext.fs.js";
import { isEmpty } from "../../../../../AppEggShellGallery/src/fable_modules/fable-library-js.5.4.0/List.js";
import { Option_getOrElse } from "../../../../../LibLangFsharp/src/OptionExtensions.fs.js";
import { tellReactArrayKeysAreOkay } from "../../../../../LibClient/src/EggShellReact.fs.js";

export function LibRouter_Components_Constructors_LR__LR_MakeRXNavigatorNavigatorRoute_Static_Z18A95B2C(prouteId, psceneConfigType, pchildren, pgestureResponseDistance, pcustomSceneConfig) {
    return new NavigatorRoute(prouteId, psceneConfigType, orElse(pgestureResponseDistance, undefined), orElse(pcustomSceneConfig, undefined));
}

export function LibRouter_Components_Constructors_LR__LR_RXNavigator_Static_2F5AD263(ref, renderScene, children, xLegacyStyles) {
    const __props = new Props(ref, renderScene);
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

