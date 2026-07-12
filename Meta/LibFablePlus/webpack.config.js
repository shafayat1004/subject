const fs = require("fs");
const os = require("os");
const path = require("path");
const webpack = require("webpack");

const TOOL_PATH = process.env.TOOL_PATH;
const PROJECT_PATH = process.env.PROJECT_PATH;
const BUNDLE_ENTRY_PATH = process.env.BUNDLE_ENTRY_PATH;
const BUNDLE_OUTPUT_PATH = process.env.BUNDLE_OUTPUT_PATH;

const mode = process.env.NODE_ENV === "development" ? "development" : "production";
const isDev = mode === "development";
console.log(`Bundling for ${mode}...`);

console.log(`Project path is: ${PROJECT_PATH}`);
console.log(`Bundle entry path is: ${BUNDLE_ENTRY_PATH}`);
console.log(`Bundle output path is: ${BUNDLE_OUTPUT_PATH}`);
console.log(`Tool path is: ${TOOL_PATH}`);

const libClientNodeModules = safeJoin(findDirUpwards("LibClient"), "node_modules");

console.log("LOADED CUSTOM WEBPACK CONFIG: alias", {
    asyncStorage: libClientNodeModules
        ? path.join(libClientNodeModules, "@react-native-async-storage/async-storage/lib/commonjs/index.js")
        : null,
});

// Globals the react-native-reanimated / react-native-worklets stack reads at import time that
// webpack 5 does not provide for the browser. Without these the web bundle throws at boot and
// the app is stuck on "Loading…":
//   - `process.env.JEST_WORKER_ID` (worklets platformChecker) -> "ReferenceError: process is
//     not defined" (webpack does not polyfill `process`; NODE_ENV is already set by `mode`).
//   - `__DEV__` (reanimated JSReanimated) -> "ReferenceError: __DEV__ is not defined" (a React
//     Native global the Metro bundler injects but webpack does not).
// There are no other bare `process`/RN-global references in the bundled RN packages.
// See runbooks/troubleshooting.md (RN 0.86 section).
const definePlugin = new webpack.DefinePlugin({
    "process.env.JEST_WORKER_ID": "undefined",
    __DEV__:                      JSON.stringify(isDev),
});

const commonConfig = {
    // Do we want source maps in production? If so, move the source-map-loader from dev to common config.
    devtool: isDev ? "eval-source-map" : false,
    plugins: [definePlugin],
    mode: isDev ? "development" : "production",
    entry: BUNDLE_ENTRY_PATH,
    output: {
        path:     BUNDLE_OUTPUT_PATH,
        filename: isDev ? "bundle.js" : "bundle.[hash].js",
    },
    resolve: {
        alias: {
            // ThirdParty libs (e.g. recharts) ship nested react copies; force a single instance.
            react:     libClientNodeModules ? path.join(libClientNodeModules, "react")     : "react",
            "react-dom": libClientNodeModules ? path.join(libClientNodeModules, "react-dom") : "react-dom",
            // Phase 4 RNW seam: web bundles import "react-native"; webpack resolves to react-native-web.
            "react-native$": libClientNodeModules
                ? path.join(libClientNodeModules, "react-native-web")
                : "react-native-web",
            // These RN ecosystem packages ship ESM builds with extensionless relative imports that
            // Webpack 5 refuses to resolve. Alias them to their CommonJS builds for web bundles.
            "@react-native-async-storage/async-storage$": libClientNodeModules
                ? path.join(libClientNodeModules, "@react-native-async-storage/async-storage/lib/module/index.js")
                : "@react-native-async-storage/async-storage",
            "react-native-gesture-handler$": libClientNodeModules
                ? path.join(libClientNodeModules, "react-native-gesture-handler/lib/module/index.js")
                : "react-native-gesture-handler",
            "react-native-reanimated$": libClientNodeModules
                ? path.join(libClientNodeModules, "react-native-reanimated/lib/module/index.js")
                : "react-native-reanimated",
            "react-native-worklets$": libClientNodeModules
                ? path.join(libClientNodeModules, "react-native-worklets/lib/module/index.js")
                : "react-native-worklets",
        },
        extensions: [".web.js", ".web.jsx", ".web.ts", ".web.tsx", ".js", ".jsx", ".ts", ".tsx", ".json"],
        modules: [
            "node_modules",
            safeJoin(findDirUpwards("LibClient"),                         "node_modules"),
            safeJoin(findDirUpwards("LibRouter"),                         "node_modules"),
            safeJoin(findDirUpwards("LibUiSubject"),                      "node_modules"),
            safeJoin(findDirUpwards("LibUiIdentityAuth"),                 "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "ImagePicker",         "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "Map",                 "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "FacebookPixel",       "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "Mapview",             "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "ReactNativeCodePush", "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "Recharts",            "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "Showdown",            "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "Something",           "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "SyntaxHighlighter",   "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "GoogleAnalytics",     "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "ReCaptcha",           "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "QRCode",              "node_modules"),
            safeJoin(findDirUpwards("ThirdParty"), "ReactLeafletOsmMap",  "node_modules"),
        ].filter(Boolean)
    },
    module: {
        rules: [
            // Handle external css files
            // This is required for Amazon Rekognition
            {
                test: /\.css$/i,
                use: ['style-loader', 'css-loader'],
            },
            // Several RN ecosystem packages (e.g. @react-native-async-storage/async-storage,
            // react-native-gesture-handler) ship ESM builds that omit file extensions on
            // relative imports. Webpack 5 treats those as fully-specified and fails to resolve
            // them; force javascript/auto and relax fullySpecified for node_modules JS.
            {
                test: /\.m?js$/,
                include: /node_modules/,
                type: "javascript/auto",
                resolve: { fullySpecified: false },
            },
        ]
    }
}

// CONFIG FOR DEVELOPMENT ==========
if (isDev) {
    const devServerPort = getDevServerPort();
    module.exports = {
        ...commonConfig,
        devServer: {
            // Listen on all interfaces so other devices on the LAN can open dev-web.
            host: "0.0.0.0",
            allowedHosts: "all",
            static: {
                directory: path.join(PROJECT_PATH, "public-dev"),
            },
            historyApiFallback: {
                disableDotRule: true
            },
            port:               devServerPort,
            client: {
                // HMR websocket follows the host:port the browser used (any interface IP).
                webSocketURL: "auto://0.0.0.0:0/ws",
                overlay: {
                    errors:   true,
                    warnings: false, // until #1116 is addressed
                }
            },
            onListening: (devServer) => {
                if (!devServer) {
                    return;
                }
                logDevServerUrls(devServerPort);
            },
            headers: {
                "Cross-Origin-Opener-Policy": "same-origin",
                "Cross-Origin-Resource-Policy": "same-origin",
            },
        },
        module: {
            ...commonConfig.module,
            rules: commonConfig.module.rules.concat({
                test: /\.js$/,
                exclude: /node_modules/,
                enforce: "pre",
                use: [resolveNpmPackage("source-map-loader")],
            }),
        },
    };

// CONFIG FOR PRODUCTION ============
} else {
    const TerserPlugin = require(resolveNpmPackage("terser-webpack-plugin"));
    module.exports = {
        ...commonConfig,
        optimization: {
            emitOnErrors:   false,
            minimize:       true,
            // NOTE without this explicitly defined minimizer, whatever the webpack defaults
            // happen to be mangle the Fable-generated code in such a way that there's an
            // inifite loop, most likely in LibClient/Styles.fs#merge. I thought I'd put this
            // minimizer in here explicitly and make it harsher and harsher until I repro the
            // infinite loop, but I failed to repro it, even though the bundle size decreased
            // to slightly below the level we get with the defaults. So it stays here, without
            // us knowning exactly what was going on.
            minimizer: [new TerserPlugin({
                terserOptions: {
                    ecma:            undefined,
                    warnings:        false,
                    parse:           {},
                    compress:        {},
                    mangle:          true, // Note `mangle.properties` is `false` by default.
                    module:          false,
                    output:          null,
                    toplevel:        true,
                    nameCache:       null,
                    ie8:             false,
                    keep_classnames: false,
                    keep_fnames:     false,
                    safari10:        false,
                    sourceMap:       true,
                },
            })],
            // I couldn't figure out how to get the app started when it's chunked,
            // so leaving this commented out for now. We anyway need to do some more
            // work to figure out chunking/caching optimizations.
            // splitChunks: {
            //     chunks: "all"
            // }
            splitChunks: false,
            runtimeChunk: false,
        },
        plugins: [
            definePlugin,
            // This plugin is used to ensure that the output bundle is a single file.
            // Sometimes webpack creates multiple chunks for runtime imports,
            // which can cause issues with our setup.
            new webpack.optimize.LimitChunkCountPlugin({
                maxChunks: 1,
            }),
        ],
    };
}

function safeJoin(base, ...rest) {
    return base != null ? path.join(base, ...rest) : null;
}

function findDirUpwards(targetDir, baseDir) {
    const currentDir = baseDir || PROJECT_PATH;
    const parentDir = path.dirname(currentDir);
    if (parentDir === currentDir) return null; // reached filesystem root
    const dir = path.join(parentDir, targetDir);
    return fs.existsSync(dir) ? dir : findDirUpwards(targetDir, parentDir);
}

function resolveNpmPackage(package) {
    return path.join(TOOL_PATH, "node_modules", package);
}

function getDevServerPort() {
    const appName = path.basename(PROJECT_PATH);
    switch (appName) {
        // TODO: get rid of these hardcodes
        case "AppEggShellGallery":        return 8082;
        default:                          return 9080;
    }
}

function isIPv4(net) {
    return net.family === "IPv4" || net.family === 4;
}

function isIPv6(net) {
    return net.family === "IPv6" || net.family === 6;
}

function formatHostForUrl(address, family) {
    if (family === "IPv6" || family === 6) {
        // Strip zone id (e.g. fe80::1%en0) — not valid in URL host literals.
        const bare = address.split("%")[0];
        return `[${bare}]`;
    }
    return address;
}

/** Print every address this machine can use to reach dev-web on the given port. */
function logDevServerUrls(port) {
    const urls = new Set([
        `http://127.0.0.1:${port}`,
        `http://localhost:${port}`,
    ]);

    for (const ifaces of Object.values(os.networkInterfaces())) {
        if (!ifaces) {
            continue;
        }
        for (const net of ifaces) {
            if (isIPv4(net) || isIPv6(net)) {
                const host = formatHostForUrl(net.address, net.family);
                urls.add(`http://${host}:${port}`);
            }
        }
    }

    console.log("\nDev server listening on all interfaces (0.0.0.0). Reachable at:\n  "
        + [...urls].sort().join("\n  ")
        + "\n");
}
