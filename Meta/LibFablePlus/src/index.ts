import * as fs from "fs-extra";
import * as path from "path";
import { spawn, SpawnOptionsWithoutStdio } from "child_process";
import { AppProject, BuildConfig, BuildTarget } from "../../LibEggshell";

function spawnRedirectingStreamsToCurrentProcess(command: string, args?: string[], options?: SpawnOptionsWithoutStdio, callBack?: (stdout: string)=>void): Promise<{exitCode: number}> {
    return new Promise((resolve, reject) => {
        const spawnedProcess = spawn(command, args, {
            ...options,
            stdio: callBack? "pipe" : "inherit"
        });

        if( callBack ) {
            spawnedProcess.stdout.on('data', (data) => {
                console.log(`${data}`);
                callBack(`${data}`);
            });
            spawnedProcess.stderr.on('data', (data) => {
                console.error(`${data}`);
                callBack(`${data}`)
            })
        }

        spawnedProcess.on("exit", exitCode => {
            resolve({exitCode});
        });

        spawnedProcess.on("error", reject);
    });
}

async function runProcess(opts: { cwd: string, command: string, args: string[], env: { [key: string]: string }, onError: (()=>void) }, callBack?: (stdout: string)=>void): Promise<void> {
    let { cwd, command, args, env } = opts;
    console.log(`${path.relative(".", cwd)}> ${command} ${args.join(" ")}`);
    const result = await spawnRedirectingStreamsToCurrentProcess(command, args, {
        cwd,
        env: Object.assign({}, process.env, env)
    }, callBack);
    console.log(`Process exited with code ${result.exitCode}`);
    if (result.exitCode !== 0) {
        opts.onError();
        return Promise.reject(result.exitCode);
    }
}

function findUpwards(baseDir: string, targetPath?: string): string {
    if (targetPath == null) {
        targetPath = baseDir;
        baseDir = __dirname;
    }
    const parentDir = path.join(baseDir, "..");
    const dir = path.join(parentDir, targetPath);
    return fs.existsSync(dir) ? dir : findUpwards(parentDir, targetPath);
}

const TOOL_PATH = path.dirname(findUpwards("package.json"));
const REPO_ROOT_PATH = path.dirname(findUpwards("global.json"));
const LIB_STANDARD_PATH = path.join(REPO_ROOT_PATH, "LibStandard");
const LIB_STANDARD_BUILD_PATH = (target: BuildTarget) => path.join(LIB_STANDARD_PATH, ".build", target, "fable");

/** FSharpPlus Comonad.fs uses Async.AsTask on the Fable 5 path; FSharpPlus hides AsTask behind !FABLE_COMPILER. */
function patchFSharpPlusComonadForFable5(): void {
    const home = process.env.HOME || process.env.USERPROFILE || "";
    if (!home) return;
    const filePath = path.join(home, ".nuget/packages/fsharpplus/1.6.1/fable/Control/Comonad.fs");
    if (!fs.existsSync(filePath)) return;
    let src = fs.readFileSync(filePath, "utf8");
    if (src.includes("EGGSHELL_FABLE5_COMONAD_PATCH")) return;
    const patched = src.replace(
            /static member\s+Extract \(x: Async<'T>\) =[\s\S]*?(?=static member\s+Extract \(x: Lazy)/,
            `static member        Extract (x: Async<'T>) =
        failwith "Comonad Extract on Async is unused on Fable (EGGSHELL_FABLE5_COMONAD_PATCH)"
    `
        );
    if (patched !== src) {
        fs.writeFileSync(filePath, patched);
        console.log(`Patched FSharpPlus Comonad for Fable 5: ${filePath}`);
    }
}

/** FSharpPlus Extensions.fs declares a GetSlice type extension on IEnumerable<'T>. .NET 9 gave the BCL
 *  IEnumerable<'T> type parameter an `allows ref struct` constraint, and F# has no syntax to declare that
 *  constraint on a type extension, so the extension fails to compile (FS0957/FS0341) once Fable compiles
 *  FSharpPlus source against net9+ reference assemblies (Fable uses the running SDK's refs regardless of
 *  the project TFM). The extension is unused here, so drop it. See dotnet/fsharp#18001. */
function patchFSharpPlusExtensionsForNet9(): void {
    const home = process.env.HOME || process.env.USERPROFILE || "";
    if (!home) return;
    const filePath = path.join(home, ".nuget/packages/fsharpplus/1.6.1/fable/Extensions/Extensions.fs");
    if (!fs.existsSync(filePath)) return;
    const src = fs.readFileSync(filePath, "utf8");
    if (src.includes("EGGSHELL_NET9_IENUMERABLE_SLICE_PATCH")) return;
    const block =
        "    type Collections.Generic.IEnumerable<'T> with\n" +
        "        member this.GetSlice = function\n" +
        "            | None  , None   -> this\n" +
        "            | Some a, None   -> this |> Seq.skip a\n" +
        "            | None  , Some b -> this |> Seq.take b\n" +
        "            | Some a, Some b -> this |> Seq.skip a |> Seq.take (b-a+1)";
    const marker = "    // EGGSHELL_NET9_IENUMERABLE_SLICE_PATCH removed IEnumerable GetSlice extension (FS0957 vs net9 allows ref struct)";
    if (src.includes(block)) {
        fs.writeFileSync(filePath, src.replace(block, marker));
        console.log(`Patched FSharpPlus Extensions for net9+: ${filePath}`);
    }
}

function getNodeModulesBin(exeFile: string): string {
    return path.join(TOOL_PATH, "node_modules", ".bin", exeFile);
}

function getFableCompiledJsEntryPath(project: AppProject, target: BuildTarget): string {
    return path.join(
        project.buildPath(target, "fable"),
        project.entryFileNameWithoutExtension + ".js"
    );
}

function getEnv(project: AppProject, target: BuildTarget, config: BuildConfig): { [key: string]: string } {
    return {
        TOOL_PATH,
        PROJECT_PATH: project.rootPath,
        BUNDLE_ENTRY_PATH: getFableCompiledJsEntryPath(project, target),
        BUNDLE_OUTPUT_PATH: project.buildPath(target, "bundle"),
        NODE_ENV: config === "dev" ? "development" : "production",
    }
}

function getRunArgs(project: AppProject, target: BuildTarget, config: BuildConfig): string[] {
    if (target === "web") {
        // If the project contains a webpack.config.js file, let Webpack use it
        // If not, use the predefined config that comes with this package.
        const webpackConfigFile = "webpack.config.js";
        const customWebpackConfigPath = path.join(project.rootPath, webpackConfigFile);

        const webpackConfigPath =
            fs.existsSync(customWebpackConfigPath)
            ? customWebpackConfigPath
            : path.join(TOOL_PATH, webpackConfigFile);

        const webpackBinPath = getNodeModulesBin(config === "dev" ? "webpack-dev-server" : "webpack")

        return ["--run", webpackBinPath, "--config", webpackConfigPath];
    } else {
        // Metro, the bundler for React Native, only understands commonjs modules,
        // so we need to do an extra transformation with babel
        const runArgs = [
            "--run",
            getNodeModulesBin("babel"),
            project.buildPath(target, "fable"),
            "--out-dir",
            project.buildPath(target, "commonjs"),
            "--plugins",
            "@babel/plugin-transform-modules-commonjs"
        ];
        return config === "dev" ? runArgs.concat("--watch") : runArgs;
    }
}

function getFableDotnetCommand(): string[] {
    // Fable development version
    // return ["run", "-c", "Release", "--project", path.join(REPO_ROOT_PATH, "../Fable/src/Fable.Cli"), "--"]
    return ["fable"];
}

function getFableArgs(project: AppProject, target: BuildTarget, config: BuildConfig, precompiledLib: string, noCache: boolean): string[] {
    return getFableDotnetCommand()
        .concat([
            project.srcPath,
            "-o",
            project.buildPath(target, "fable"),
            "--exclude",
            "FablePlugins"
        ])
        .concat(precompiledLib ? [
            "--precompiledLib",
            precompiledLib,
        ] : [])
        .concat(noCache === true ? [
            "--noCache"
        ] : [])
        .concat(config === "dev" || config === "package" ? [
            "--define",
            "DEBUG",
        ] : [])
        .concat(config === "dev" ? [
            // Source maps
            "-s",
            "--watch",
            // Increase watch delay to wait for RenderDSL
            "--watchDelay",
            "500"
        ] : [])
        .concat(target === "web" ? [
            "--define",
            "EGGSHELL_PLATFORM_IS_WEB"
        ] : [])
        .concat(
            getRunArgs(project, target, config)
        );
}

async function precompileLibStandard(target: BuildTarget): Promise<string> {
    patchFSharpPlusComonadForFable5();
    patchFSharpPlusExtensionsForNet9();
    const precompiledLib = LIB_STANDARD_BUILD_PATH(target);
    const args = getFableDotnetCommand()
        .concat([
            "precompile",
            path.join(LIB_STANDARD_PATH, "src"),
            "-o",
            precompiledLib,
            "--exclude",
            "FablePlugins"
        ])
        .concat(target === "web" ? [
            "--define",
            "EGGSHELL_PLATFORM_IS_WEB"
        ] : []);

    await runProcess({
        command: "dotnet",
        args,
        cwd: LIB_STANDARD_PATH,
        // TODO: Take BuildConfig into account? For this we would need 2 separate LibStandard compilations
        env: { NODE_ENV: "production" },
        onError: () => {
            console.error("Fable precompilation failed, removing build dir");
            fs.removeSync(precompiledLib);
        }
    });

    return precompiledLib;
}

export default async function runFable(project: AppProject, target: BuildTarget = "web", config: BuildConfig = "dev", noPrecompile: boolean, noCache: boolean, callBack?: (stdout: string)=>void) : Promise<void> {
    patchFSharpPlusComonadForFable5();
    patchFSharpPlusExtensionsForNet9();
    // There are issues with Fable precompilation and Metro bundler, disable the feature for now
    // in native platform until further investigation.
    noPrecompile = target === "web" ? noPrecompile : true;

    const precompiledLib = !noPrecompile ? await precompileLibStandard(target) : null;

    return runProcess({
        command: "dotnet",
        args: getFableArgs(project, target, config, precompiledLib, noCache),
        cwd: project.rootPath,
        env: getEnv(project, target, config),
        onError: () => {
            console.error("Fable compilation failed, removing build dir");
            fs.removeSync(project.buildPath(target, "fable"));
            if (precompiledLib) {
                fs.removeSync(precompiledLib);
            }
        }
    }, callBack);
}
