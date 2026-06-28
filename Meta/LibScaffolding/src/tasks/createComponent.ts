import * as fs       from "fs";
import * as gulp     from "gulp";
import * as path     from "path";
import * as inquirer from "inquirer";

import { Promises, Seq } from "eggshell-lib-lang-typescript";

import { Project } from "eggshell-lib-eggshell";
import { getTemplatesDir, templateFile } from "../templating";
import { validatePascalCase } from "../inquirerHelpers";

interface Answers {
    name:                string
    type:                'stateless' | 'estateful' | 'pstateful' | 'functional'
    maybeTypeParameters: string
}

type TypeParameters = {
    WithConstraints:    string
    WithoutConstraints: string
}

export function createComponent(closestUptreeProject: Project) : Promise<void> {
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
                    message:  "Component name",
                    validate: validatePascalCase,
                },
                {
                    name:     "type",
                    type:     "list",
                    choices: [
                        { value: "stateless",  name: "Stateless",                 short: "stateless"  },
                        { value: "estateful",  name: "With ephemeral state only", short: "estateful"  },
                        { value: "pstateful",  name: "With persistent state",     short: "pstateful"  },
                        { value: "functional", name: "Functional component",      short: "functional" },
                    ],
                    message:  "Type",
                },
                {
                    name:    "maybeTypeParameters",
                    type:    "input",
                    message: "Type parameters (e.g. \"'Item\" or \"'T, 'U\", without the quotes; leave blank for none)",
                },
            ]);
            return actuallyCreateComponent(answers, closestUptreeProject.config.name, closestUptreeProject.srcPath, templatesDir);
        },
        Promise.reject
    );
}

async function actuallyCreateComponent(answers: Answers, appName: string, srcDir: string, templatesDir: string) : Promise<void> {
    const rawName = answers.name;

    const mutableNameParts = rawName.split(".")
    const nameLeaf         = mutableNameParts.pop()
    const namePrefix       = mutableNameParts

    let typeParameters : (TypeParameters | undefined) = undefined
    let numberOfParameters : number | undefined = undefined
    if(answers.maybeTypeParameters){
        typeParameters = {
            WithConstraints:    answers.maybeTypeParameters,
            WithoutConstraints: answers.maybeTypeParameters.replace(/ +when.*$/, "")
        }
        numberOfParameters = typeParameters.WithoutConstraints.split(',').length
    }

    const data = {
        projectName:         appName,
        componentName:       rawName,
        componentNameLeaf:   nameLeaf,
        type:                answers.type,
        typeParameters,
        numberOfParameters
    };

    const componentDirectory = path.join(srcDir, "Components", ...namePrefix, nameLeaf);

    if (fs.existsSync(componentDirectory)) {
        return Promise.reject(`Directory for component name already exists: ${componentDirectory}`)
    }

    const getTemplateFileFromType = ({type} : Answers) => {
        switch (type) {
            case "stateless":
                return [
                    templateFile(gulp, `${templatesDir}/component/type/stateless/Component.fs.template`, `${componentDirectory}/${nameLeaf}.fs`, data),
                ]
            case "estateful":
                return [
                    templateFile(gulp, `${templatesDir}/component/type/estateful/Component.fs.template`, `${componentDirectory}/${nameLeaf}.fs`, data),
                ]
            case "pstateful":
                return [
                    templateFile(gulp, `${templatesDir}/component/type/pstateful/Component.fs.template`, `${componentDirectory}/${nameLeaf}.fs`, data),
                ]
            case "functional":
                return [
                    templateFile(gulp, `${templatesDir}/component/type/functional/Component.fs.template`, `${componentDirectory}/${nameLeaf}.fs`, data),
                ]
        }
    }
    const promiseMakers: Array<() => Promise<void>> = [
        ...getTemplateFileFromType(answers)
    ];

    await Promises.inSeries(promiseMakers);

    console.log(generateFinalMessage(namePrefix, nameLeaf));
}

function generateFinalMessage(namePrefix: Seq<string>, nameLeaf: string) : string {
    let namePrefixDir =
        namePrefix.length === 0
        ? ""
        : namePrefix.join("/") + "/"

    return [
        "",
        "",
        "",
        "Component created.",
        "",
        "Now you need to add it to your .fsproj file, just above the components that",
        "will be referencing this one is a good place. This part isn't automated yet, sorry.",
        "",
        "",
        `    <Compile Include=\"Components/${namePrefixDir}${nameLeaf}/${nameLeaf}.fs\" />`,
        "",
        "",
    ]
    .filter(_ => _ !== undefined)
    .join("\n");
}