#!/bin/zsh
# Web dev-loop preflight: server up + bundle freshness. See runbooks/web.md.
# usage: web-preflight.sh [port] [appdir]
set -u
PORT="${1:-8082}"
APP="${2:-AppEggShellGallery}"
if curl -sf --max-time 3 "http://localhost:$PORT/" > /dev/null; then
  echo "dev-web OK on :$PORT"
else
  echo "FAIL: nothing on :$PORT. Gallery: cd AppEggShellGallery && ../eggshell dev-web (8082); AppTodo: cd SuiteTodo/AppTodo && ../../eggshell dev-web (9080)"
  exit 0
fi
if [[ -d "$APP/.build/web/fable" ]]; then
  NEWSRC=$(find "$APP/src" -name "*.fs" -newer "$APP/.build/web/fable" 2>/dev/null | head -5)
  if [[ -n "$NEWSRC" ]]; then
    echo "WARN: sources newer than web build output (stale bundle?); run fable-rebuild-verify:"
    echo "$NEWSRC"
  else
    echo "bundle freshness OK"
  fi
fi
