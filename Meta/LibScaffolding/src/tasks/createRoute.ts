import * as fs       from "fs";
import * as gulp     from "gulp";
import * as path     from "path";
import * as inquirer from "inquirer";
import * as _        from "lodash";

import { QQQ, Seq, ifMatch, Promises } from "eggshell-lib-lang-typescript";

import { Project } from "eggshell-lib-eggshell";
import { getTemplatesDir, templateFile } from "../templating";
import { validatePascalCase } from "../inquirerHelpers";

type Answers = {
    name:          string
    hasParameters: boolean
}

export function createRoute(closestUptreeProject: Project) : Promise<void> {
    if (closestUptreeProject.type !== 'app' && closestUptreeProject.type !== 'library') {
        return Promise.reject("Can only create component in an app or library, check your pwd");
    }

    return getTemplatesDir(closestUptreeProject)
    .match(
        async (templatesDir) => {
            const answers: Answers = await inquirer.prompt([
                {
                    name:     "name",
                    type:     "input",
                    message:  "Route name",
                    validate: validatePascalCase,
                },
                {
                    name:    "hasParameters",
                    type:    "confirm",
                    message: "Will your route have parameters?",
                },
            ]);
            let routeName = answers.name.startsWith("Route")
                          ? answers.name.substr(5)
                          : answers.name
            return actuallyCreateRoute(routeName, closestUptreeProject.config.name, answers.hasParameters, closestUptreeProject.srcPath, templatesDir);
        },
        Promise.reject
    );
}

async function actuallyCreateRoute(rawRouteName: string, appName: string, hasParameters: boolean, srcDir: string, templatesDir: string) : Promise<void> {
    const mutableNameParts = rawRouteName.split(".")
    const nameLeaf   = mutableNameParts.pop()
    const namePrefix = mutableNameParts

    const data = {
        appName,
        componentName:     rawRouteName,
        componentNameLeaf: nameLeaf,
        hasParameters,
    };

    const componentDirectory = path.join(srcDir, "Components", "Route", ...namePrefix, nameLeaf);

    if (fs.existsSync(componentDirectory)) {
        return Promise.reject(`Directory for component name already exists: ${componentDirectory}`)
    }

    await Promises.inSeries([
        templateFile(gulp, `${templatesDir}/route/Route.fs.template`, `${componentDirectory}/${nameLeaf}.fs`, data),
    ]);

    console.log(generateFinalMessage(rawRouteName, namePrefix, nameLeaf, hasParameters));
}

function generateFinalMessage(rawRouteName: string, namePrefix: Seq<string>, nameLeaf: string, hasParameters: boolean) : string {
    const routeUrl = rawRouteName.replace(".", "/")

    let namePrefixDir =
        namePrefix.length === 0
        ? ""
        : namePrefix.join("/") + "/"

    const rawLines: Seq<string | Seq<string>> = [
        "",
        "",
        "",
        "Route component created.",
        "",
        "Now you need to add it to your .fsproj file, together with other routes.",
        "This part isn't automated yet, sorry.",
        "",
        "",
        `    <Compile Include=\"Components/Route/${namePrefixDir}${nameLeaf}/${nameLeaf}.fs\" />`,
        "",
        "",
        "Next, paste this union case into the Route type of your Navigation.fs file:",
        "(you'll have to make your own adjustments for namespaced nested routes)",
        "",
        ifMatch(hasParameters,
            /*  if  */ () => [
                `| ${rawRouteName} of SomeParam: int`
            ],
            /* else */ () => [
                `| ${rawRouteName}`
            ]
        ),
        "",
        "",
        "And then update the routes human readable mapping spec, adding the new case",
        "",
        "",
        ifMatch(hasParameters,
            /*  if  */ () => [
                `            ("/${routeUrl}/{json}",`,
                `                (fun parts -> ${rawRouteName} (parts.GetFromJson 0)),`,
                `                (function (${rawRouteName} p) -> Some [Json.ToString p] | _ -> None))`,
            ],
            /* else */ () => [
                `            ("/${routeUrl}",`,
                `                (fun _ -> ${rawRouteName}),`,
                `                (function (${rawRouteName} _) -> Some [] | _ -> None))`,
            ]
        ),
        "",
        "",
        "Finally, go to App.fs, and add a match arm for the new route in the Authenticated component:",
        "",
        "",
        ifMatch(hasParameters,
            /*  if  */ () => [
                `                     | ${rawRouteName} someParam -> Ui.Route.${rawRouteName} someParam`,
            ],
            /* else */ () => [
                `                     | ${rawRouteName} -> Ui.Route.${rawRouteName} ()`,
            ]
        ),
        "",
        "",
    ]
    .filter(_ => _ !== undefined);

    return _.flatten(rawLines).join("\n");
}
