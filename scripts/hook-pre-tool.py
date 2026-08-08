#!/usr/bin/env python3
"""agent-os PreToolUse check (flag-based, stdin-safe).

Parses the Claude Code PreToolUse JSON from stdin and calls the
flag-based `agentos check-path` / `agentos check-command` subcommands
instead of piping stdin to `agentos hook-pre-tool`.

Why this exists: `agentos hook-pre-tool` does an unconditional
`sys.stdin.read()` (agent-os cli.py:171). Claude Code pipes the payload
and closes stdin, so the read gets EOF and returns. But a harness that
spawns this wrapper with a never-EOF inherited stdin (e.g. Bun's `$`
shell, the same class of bug as codemem 1978's hook-post-tool freeze)
would hang forever inside that read. This helper reads stdin ONCE here
(it gets EOF from Claude Code, which is safe), then calls the flag-based
subcommands that never touch stdin. It mirrors the opencode plugin
(.opencode/plugins/agentos.js) which already uses check-path /
check-command for the same reason.

Exit codes (same contract as the hook-pre-tool path):
    0  allow
    1  warn (ask_first, undeclared path, warn rule)
    2  block (never path, deny command)
Missing or malformed input fails open (exit 0).
"""
import json
import os
import select
import subprocess
import sys


def _read_stdin_bounded(timeout=10.0):
    """Read available stdin data without waiting for EOF.

    Uses select to wait up to `timeout` seconds for data, then drains
    the buffer in non-blocking chunks. This avoids the hang that
    sys.stdin.read() causes when stdin is inherited and never gets EOF
    (the codemem 1978 class of bug). Returns the decoded text, or ""
    when no data arrives within the timeout (fail open).
    """
    ready, _, _ = select.select([sys.stdin], [], [], timeout)
    if not ready:
        return ""  # no data within timeout: fail open, do not hang
    chunks = []
    while True:
        ready, _, _ = select.select([sys.stdin], [], [], 0.05)
        if not ready:
            break
        chunk = sys.stdin.buffer.read1(65536)
        if not chunk:
            break
        chunks.append(chunk)
    return b"".join(chunks).decode("utf-8", errors="replace")


def main():
    agentos = sys.argv[1] if len(sys.argv) > 1 else ".agent-os/bin/agentos"
    try:
        stdin_text = _read_stdin_bounded()
    except (OSError, ValueError):
        return 0
    try:
        payload = json.loads(stdin_text) if stdin_text.strip() else {}
    except ValueError:
        return 0  # unparseable: fail open

    tool_input = payload.get("tool_input") or {}
    tool_name = payload.get("tool_name") or payload.get("tool") or ""
    target = tool_input.get("file_path") or tool_input.get("notebook_path")
    command = tool_input.get("command")

    worst = 0
    if target:
        result = subprocess.run([agentos, "check-path", target],
                                capture_output=True, text=True)
        msg = (result.stderr or result.stdout).strip()
        if msg:
            print(msg, file=sys.stderr)
        worst = max(worst, result.returncode)
    if command:
        result = subprocess.run([agentos, "check-command",
                                 "--tool", tool_name or "bash",
                                 "--command", command],
                                capture_output=True, text=True)
        msg = (result.stderr or result.stdout).strip()
        if msg:
            print(msg, file=sys.stderr)
        worst = max(worst, result.returncode)
    return worst


if __name__ == "__main__":
    sys.exit(main())
