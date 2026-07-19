---
name: symptom-matcher
description: When an unfamiliar build/runtime error appears, run scripts/symptom-match.sh to match it against troubleshooting.md BEFORE improvising root-cause analysis. If matched, follow the linked fix; if the fix works and there was no match, add a troubleshooting entry (maintaining-docs.md rules).
user-invocable: true
argument-hint: "<error-log-file> or "verbatim error string""
---

# symptom-matcher

Match build/runtime errors against the symptom-to-fix catalog in `troubleshooting.md`.

## When to use

- An unfamiliar error appears in the build log, Metro console, or device logcat.
- You suspect a known gotcha but don't recall the fix.
- You fixed an error and want to document it for the next person.

## Never do this

- Guess the fix without checking the catalog.
- Assume an error is new without running the matcher.

## Procedure

1. Run:
   ```bash
   scripts/symptom-match.sh <error-log-file>
   # or
   scripts/symptom-match.sh "verbatim error string"
   ```

2. If matched:
   - Follow the linked fix in the output.
   - If the fix works, you're done.

3. If no match:
   - Fix the error.
   - Add a troubleshooting entry per `maintaining-docs.md` (section: "Adding a troubleshooting entry").
   - Run `docs-sync` to update the gallery.

## Proof gate

- Matcher output shows section header + matching lines.
- Fix resolves the error.
- New entry appears in `troubleshooting.md` (if added).

## Doc refs

- `AppEggShellGallery/public-dev/docs/runbooks/troubleshooting.md`
- `AppEggShellGallery/public-dev/docs/maintaining-docs.md`
- `scripts/symptom-match.sh`