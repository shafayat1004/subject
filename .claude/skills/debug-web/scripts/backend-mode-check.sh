#!/bin/zsh
# Quick check: is AppTodo's dev-web actually serving fake in-memory todos or wired to a
# real backend, and if real, is that backend actually reachable? See runbooks/web.md #fake-service.
# Don't assume from the default (fake) or from a stale mental model -- read the SERVED config,
# since that's what the running browser bundle actually uses.
# usage: backend-mode-check.sh [port] [appdir]
set -u
PORT="${1:-9080}"
APP="${2:-SuiteTodo/AppTodo}"

echo "-- served config (http://127.0.0.1:$PORT/configSourceOverrides.dev.js) --"
SERVED=$(curl -sf --max-time 3 "http://127.0.0.1:$PORT/configSourceOverrides.dev.js")
if [[ -z "$SERVED" ]]; then
  echo "FAIL: nothing on :$PORT. Start dev-web first (see runbooks/web.md #start)."
  exit 1
fi
LINE=$(echo "$SERVED" | grep 'configSourceOverrides\.BackendUrl')
echo "$LINE"

# On-disk copies, for comparison -- the served one wins if they disagree (stale local edit).
if [[ -f "$APP/configSourceOverrides.dev.js" ]]; then
  APPROOT_LINE=$(grep 'configSourceOverrides\.BackendUrl' "$APP/configSourceOverrides.dev.js" 2>/dev/null)
  if [[ "$APPROOT_LINE" != "$LINE" ]]; then
    echo "NOTE: app-root configSourceOverrides.dev.js differs from served public-dev/ copy (served wins; app-root is not what the browser loads):"
    echo "  $APPROOT_LINE"
  fi
fi

if echo "$LINE" | grep -q '^//'; then
  echo "MODE: FAKE (in-memory todos; BackendUrl commented out). No backend needed."
  exit 0
fi

URL=$(echo "$LINE" | sed -E 's/.*"(http[^"]+)".*/\1/')
echo "MODE: configured for REAL backend at $URL -- checking reachability..."
CODE=$(curl -s -o /dev/null --max-time 3 -w "%{http_code}" -X POST "$URL/api/v1/realTime/negotiate?negotiateVersion=1")
if [[ "$CODE" =~ ^2 ]]; then
  echo "REAL backend reachable (negotiate -> $CODE). App should load real SQL-backed todos."
else
  echo "REAL backend NOT reachable (negotiate -> ${CODE:-no response}). App will hang on \"Loading...\"."
  echo "Fix: start the backend (see runbooks/troubleshooting.md #suitetodo-devstack), or comment"
  echo "BackendUrl back out in $APP/public-dev/configSourceOverrides.dev.js to fall back to fake mode."
fi
