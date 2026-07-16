import * as path from "path";
import * as chokidar from "chokidar";
import * as fs from "fs";
import * as glob from "glob-promise";
const fsPromises = fs.promises;

import { childProcessPlus } from "../../../LibNode/dist";
import { getRepoRootProject, AppProject, Project } from "../../LibEggshell/dist";
import runFable from "../../LibFablePlus";

// Copy from `images/` directly — `public-dev/images` is a symlink and gulp copy corrupts binaries.
export const nativeAppImageAssetGlobbingPath = "images/**/*.{png,jpg,gif,jpeg,svg}"
export const nativeAppBuildAssetPath         = (project: AppProject) => `${project.buildRootPath}/native/assets/`

const nativeAppImagesDestPath = (project: AppProject) =>
    path.join(nativeAppBuildAssetPath(project), "public-dev/images")

async function copyImageFile(project: AppProject, relativePath: string) : Promise<void> {
    const src  = path.join(project.rootPath, relativePath)
    const dest = path.join(nativeAppImagesDestPath(project), path.relative("images", relativePath))
    await fsPromises.mkdir(path.dirname(dest), { recursive: true })
    await fsPromises.copyFile(src, dest)
}

export function transpileToJsForNative(project: AppProject, watchMode: boolean = false, onMessageCallback?: (message: string)=>void) : Promise<void> {
    return runFable(project, "native", watchMode ? "dev" : "package", false, false, onMessageCallback);
}

export async function runReactNativeCli(project: Project, isResetCacheRequested: boolean, shouldBeVerbose: boolean) : Promise<void> {
    if (project.type !== 'app') {
        return Promise.reject("Can only package an app project")
    }

    return getRepoRootProject(project)
    .match(async repoRootProject => {
        const npxCmd = /^win/.test(process.platform) ? "npx.cmd" : "npx";

        const result = await childProcessPlus.spawnRedirectingStreamsToCurrentProcess(
            npxCmd,
            [
                "react-native",
                "start",
                ... isResetCacheRequested ? ["--reset-cache"] : [],
                ... shouldBeVerbose ? ["--verbose"] : []
            ]
        )

        console.log(`npx closed with ${result.exitCode}`);

        if (result.exitCode !== 0) {
            return Promise.reject(result.exitCode);
        }
    },
    Promise.reject
    );
}

export async function copyStaticFiles(project: AppProject): Promise<void> {
    const files = await glob(nativeAppImageAssetGlobbingPath, { cwd: project.rootPath, nodir: true })
    console.log(`Copying ${files.length} native image assets from images/ -> .build/native/assets/public-dev/images/`)
    await Promise.all(files.map((relativePath: string) => copyImageFile(project, relativePath)))
}

export function startWatchNativeAssets(project: AppProject) : Promise<void> {
    // not resolving this promise, since watch only stops when ctrl+c'ed
    return new Promise(async () => {
        console.log("\n\nStart watching asset changes...\n\n");

        const chokidarOptions = {
            cwd:           project.rootPath,
            ignoreInitial: true,
        }

        const srcFileGlobbing = nativeAppImageAssetGlobbingPath
        const destPath        = nativeAppBuildAssetPath(project);

        const nativeAppAssetWatcher = chokidar.watch(srcFileGlobbing, chokidarOptions);

        const normalizeChokidarDestinationFilename = (chokidarFilename: string) : string => {
            return path.join(destPath, "public-dev/images", path.relative("images", chokidarFilename));
        };

        nativeAppAssetWatcher.on('change', async (chokidarFilename: string) => {
            logOnError(
                () => copyImageFile(project, chokidarFilename),
                `File ${chokidarFilename} was changed`
            )
        });

        nativeAppAssetWatcher.on('add', async (chokidarFilename: string) => {
            logOnError(
                () => copyImageFile(project, chokidarFilename),
                `File ${chokidarFilename} was added`
            )
        });

        nativeAppAssetWatcher.on('unlink', (chokidarFilename: string) => {
            logOnError(() =>
                fsPromises.unlink(normalizeChokidarDestinationFilename(chokidarFilename)),
                `File ${chokidarFilename} was removed`
            )
        });

    });
}

export async function reactNativeRunAndroid(project: Project, deviceId: undefined|string, variant: undefined|string) {
    return getRepoRootProject(project)
        .match( async repoRootProject => {
            if(variant) {
                console.log(`Running build with custom build variant: ${variant}`);
            }
            const npxCmd = /^win/.test(process.platform) ? "npx.cmd" : "npx";
            const result = await childProcessPlus.spawnRedirectingStreamsToCurrentProcess(
                npxCmd,
                [
                    "react-native",
                    "run-android",
                    ... deviceId ? ["--deviceId", deviceId]: [],
                    ... variant ? ["--variant", variant] : []
                ]
            )

            console.log(`react-native-cli closed with ${result.exitCode}`);

            if (result.exitCode !== 0) {
                return Promise.reject(result.exitCode);
            }
        },
        Promise.reject
    );
}

async function logOnError(action: () => Promise<void>, successMessage: string) {
    try {
        await action()
        console.log(successMessage)
    } catch(e) {
        console.error(e)
    }
}