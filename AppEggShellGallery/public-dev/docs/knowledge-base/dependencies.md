# npm Dependency Baseline

This is a point-in-time snapshot of the npm audit results taken when `./initialize` was run across the full repository. It reflects the state after the framework-wide NPM upgrade carried out during the modernization initiative (see the [Engineering Log](./engineering-log.md) entry dated 2026-06-27).

**This page is not continuously maintained.** To regenerate the numbers, run `./initialize` from the repository root and inspect the `npm audit` output per workspace. The snapshot below is provided for orientation.

---

## Per-workspace summary

| Workspace | Approx. packages | Low | Moderate | High | Critical |
|-----------|-----------------|-----|----------|------|----------|
| `Meta/AppRenderDslCompiler` (npm portion) | 3 | 0 | 0 | 0 | 0 |
| `LibLangTypeScript` | 4 | 0 | 0 | 1 | 0 |
| `LibNode` | 9 | 0 | 0 | 1 | 0 |
| `LibClient` | 414 | 1 | 7 | 6 | 1 |
| `LibRouter` | 536 | 2 | 11 | 12 | 0 |
| `LibUiAdmin` | 3 | 0 | 0 | 0 | 0 |
| `LibUiSubject` | 357 | 1 | 7 | 7 | 0 |
| `LibUiSubjectAdmin` | 3 | 0 | 0 | 0 | 0 |
| `Meta/LibEggshell` | 9 | 0 | 0 | 1 | 0 |
| `Meta/LibScaffolding` | 439 | 3 | 7 | 9 | 0 |
| `Meta/LibFablePlus` | 388 | 6 | 11 | 13 | 1 |
| `Meta/LibRtCompilerFileSystemBindings` | 614 | 7 | 12 | 16 | 2 |
| `Meta/AppEggshellCli` | 1,102 | 7 | 12 | 18 | 2 |
| `ThirdParty/SyntaxHighlighter` | 36 | 0 | 2 | 1 | 0 |
| `ThirdParty/Showdown` | 30 | 0 | 1 | 1 | 0 |
| `ThirdParty/Map` | 6 | 0 | 0 | 0 | 0 |
| `ThirdParty/ImagePicker` | 714 | 5 | 9 | 12 | 2 |
| `ThirdParty/ReactNativeCodePush` | 155 | 3 | 5 | 7 | 2 |
| `ThirdParty/ReactNativeDeviceInfo` | 4 | 0 | 0 | 0 | 0 |
| `ThirdParty/ReactNativeContacts` | 4 | 0 | 0 | 0 | 0 |
| `ThirdParty/Recharts` | 70 | 0 | 4 | 5 | 0 |
| `ThirdParty/FacebookPixel` | 663 | 5 | 9 | 15 | 2 |
| `ThirdParty/BackgroundGeolocation` | 5 | 0 | 0 | 0 | 0 |
| `ThirdParty/GoogleAnalytics` | 818 | 5 | 15 | 23 | 4 |
| `ThirdParty/SunmiPrint` | 613 | 5 | 14 | 7 | 2 |
| `LibPushNotification/Client` | 344 | 1 | 7 | 6 | 0 |

---

## Notes

- **Package counts** are the total from `npm audit` (including transitive dependencies), not direct dependencies.
- **High/critical vulnerabilities** are largely concentrated in older ThirdParty wrappers (`GoogleAnalytics`, `FacebookPixel`, `ImagePicker`, `SunmiPrint`) that bundle legacy Babel and webpack toolchains pinned to versions with known CVEs. These workspaces have no path to `npm audit fix --force` without major version breaking changes in their bundlers.
- **LibClient** and **LibRouter** have moderate/high counts primarily from their React Native and Webpack transitive chains. Post-upgrade counts are substantially lower than the pre-upgrade baseline (LibClient dropped from 414 to around 82 after removing unused devDeps in an earlier pass; the snapshot above reflects the state before that targeted cleanup was fully propagated to all workspaces).
- **Showdown** ReDoS advisory persists on 2.x (`npm audit` shows "no fix available"). It is used only for gallery Markdown rendering. Replace with `marked` later if needed.
- `Meta/AppEggshellCli` aggregates transitive dependencies from `LibEggshell`, `LibScaffolding`, `LibFablePlus`, and `LibRtCompilerFileSystemBindings`, so its count is the highest.

To regenerate:

```sh
./initialize
```

The initialize script runs `npm install` and `npm audit` in each workspace sequentially and prints the audit summary per workspace to stdout.
