---
name: scaffolding-validator
description: After `eggshell create-app` changes, scaffold a throwaway app into /var/folders/zl/0l6kmjv55glg945ss8128djc0000gp/T/opencode, verify it builds and boots web, then delete it. If create-app is not yet functional, the skill documents the procedure spec with a "BLOCKED" note.
user-invocable: true
argument-hint: ""
---

# scaffolding-validator

Validate `eggshell create-app` produces a working app.

## When to use

- After changing the `eggshell create-app` implementation.
- Before declaring Goal B (scaffolding) done.

## BLOCKED: create-app not functional yet (Goal B)

`eggshell create-app` exists but does not yet produce a buildable app. The skill documents the intended procedure for when it becomes functional.

## Procedure (spec for when unblocked)

1. Scaffold a throwaway app:
   ```bash
   TMPDIR=/var/folders/zl/0l6kmjv55glg945ss8128djc0000gp/T/opencode
   cd "$TMPDIR"
   "$REPO_ROOT/eggshell" create-app throwaway-app
   cd throwaway-app
   ```

2. Verify it builds:
   ```bash
   ./eggshell build-lib
   ```

3. Verify it boots web:
   ```bash
   ./eggshell dev-web &
   sleep 15
   curl -s http://localhost:9080 | grep -q "<title>" && echo "Web boot OK"
   pkill -f "eggshell dev-web"
   ```

4. Clean up:
   ```bash
   rm -rf "$TMPDIR/throwaway-app"
   ```

## Proof gate

- Build exits 0.
- Web serves HTML (HTTP 200).
- Throwaway is deleted.

## Doc refs

- `SuiteTodo/AppTodo/README.md` (app build/run)
- `runbooks/web.md` (dev-web)
- `maintaining-docs.md` (Goal B status)