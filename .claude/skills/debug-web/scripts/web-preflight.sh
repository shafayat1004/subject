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
  REF=$(find "$APP/.build/web/fable" -type f -print0 2>/dev/null | xargs -0 ls -t 2>/dev/null | head -1)
  if [[ -z "$REF" ]]; then
    echo "WARN: no build output files under $APP/.build/web/fable (build never ran?)"
  else
    NEWSRC=$(find "$APP/src" -name "*.fs" -newer "$REF" 2>/dev/null | head -5)
    if [[ -n "$NEWSRC" ]]; then
      echo "WARN: sources newer than newest web build output (stale bundle?); run fable-rebuild-verify:"
      echo "$NEWSRC"
    else
      echo "bundle freshness OK"
    fi
  fi
fi
