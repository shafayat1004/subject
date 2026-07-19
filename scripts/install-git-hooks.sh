#!/usr/bin/env bash
# Install repository git hooks. Idempotent.
# usage: install-git-hooks.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
HOOK_DIR="$ROOT/.git/hooks"
HOOK="$HOOK_DIR/pre-commit"

mkdir -p "$HOOK_DIR"

if [[ -f "$HOOK" ]]; then
    BACKUP="$HOOK.bak"
    if [[ ! -f "$BACKUP" ]]; then
        cp "$HOOK" "$BACKUP"
        echo "backed up existing pre-commit hook -> $BACKUP"
    else
        echo "keeping existing backup $BACKUP"
    fi
fi

cat > "$HOOK" << 'EOF'
#!/usr/bin/env bash
# Auto-installed pre-commit hook. Runs path guard + staged .fs format check.
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
"$ROOT/scripts/forbidden-path-guard.sh" --staged

STAGED_FS=()
while IFS= read -r line; do
    [[ -n "$line" ]] && STAGED_FS+=("$line")
done < <(git diff --cached --name-only --diff-filter=ACMR | grep '\.fs$' || true)
if [[ ${#STAGED_FS[@]} -gt 0 ]]; then
    if command -v dotnet >/dev/null 2>&1 && dotnet tool list --local 2>/dev/null | grep -q eggshell-fmt; then
        if ! dotnet tool run eggshell-fmt -- --check "${STAGED_FS[@]}" >/dev/null 2>&1; then
            echo "FAIL: eggshell-fmt --check found formatting issues. Run 'dotnet tool run eggshell-fmt -- <file.fs>'." >&2
            exit 1
        fi
    fi
fi

echo "REMINDER: 'git commit --no-verify' bypasses this hook."
EOF

chmod +x "$HOOK"
echo "installed pre-commit hook -> $HOOK"
