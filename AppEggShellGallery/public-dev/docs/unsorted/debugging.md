# Debugging

> Debugging and the dev loop are now covered in depth in the **Runbooks** section.

- [Runbooks → Overview & Decision Tree](../runbooks/index.md) — the "I'm stuck" decision tree and the
  two-tier observation model.
- [Runbooks → Troubleshooting](../runbooks/troubleshooting.md) — the symptom-to-fix catalog (build/cache,
  styling, RNW/ReactXP bindings, layout, color, dark-mode inputs, pickers, key warnings, gestures).
- [Runbooks → The Dev Loop](../runbooks/dev-loop.md) — edit, rebuild, confirm, reload, observe.

## Quick reference: `Throw 1` + blank white screen

If you see `Throw 1` in the browser console (usually with a blank white screen), you still have a
compile-time Fable error that scrolled out of view. Go back to the changed file, recompile, and read the
first `error FS`. See [Runbooks → Troubleshooting](../runbooks/troubleshooting.md).
