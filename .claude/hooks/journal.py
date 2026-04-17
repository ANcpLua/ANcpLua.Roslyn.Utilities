#!/usr/bin/env python3
"""Semantic activity journal for Claude Code sessions. Fails loud on bugs."""
import json
import os
import re
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

PROJECT_DIR = Path(os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd()).resolve()
JOURNAL_DIR = PROJECT_DIR / ".claude" / "journal"
JOURNAL_TOOLS = {"Edit", "Write", "NotebookEdit", "Bash"}
_SUFFIX_CAT = (
    (".props", "msbuild_props"), (".targets", "msbuild_targets"),
    (".csproj", "tree_structure"), (".slnx", "tree_structure"),
    (".md", "docs"), (".editorconfig", "editorconfig"),
)


def now_iso() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def classify_edit(path: str) -> str:
    p = path.replace("\\", "/").lower()
    name = p.rsplit("/", 1)[-1]
    if name in ("directory.packages.props", "version.props"):
        return "package_versions"
    if name in ("claude.md", "agents.md") or "/.claude/" in p:
        return "agent_instructions"
    if p.endswith(".cs"):
        return "roslyn_tests" if re.search(r"(?:^|/)tests?[./]|\.tests?\.", p) else "roslyn_symbols"
    for sfx, cat in _SUFFIX_CAT:
        if p.endswith(sfx):
            return cat
    return "other"


def classify_bash(cmd: str) -> str:
    cmd = cmd.strip()
    if not cmd:
        return "other"
    if re.match(r"^git(\s|$)", cmd):
        return "git_op"
    if re.match(r"^(npm|pnpm|yarn|bun|cargo|pip|uv)(\s|$)", cmd):
        return "nuget_op"
    m = re.match(r"^dotnet\s+(\w+)", cmd)
    if not m:
        return "script_op"
    sub = m.group(1)
    if sub in ("add", "remove", "restore", "update") and "package" in cmd:
        return "nuget_op"
    if sub in ("add", "remove") and "reference" in cmd:
        return "tree_structure"
    if sub in ("build", "pack", "publish", "msbuild"):
        return "build_op"
    if sub == "test":
        return "test_op"
    if sub in ("restore", "new", "tool"):
        return "nuget_op"
    return "script_op"


def relpath(raw: str) -> str:
    if not raw:
        return ""
    p = Path(raw)
    full = p.resolve() if p.is_absolute() else (PROJECT_DIR / p).resolve()
    return str(full.relative_to(PROJECT_DIR)) if full.is_relative_to(PROJECT_DIR) else str(full)


def emit(sid: str, row: dict) -> None:
    JOURNAL_DIR.mkdir(parents=True, exist_ok=True)
    with (JOURNAL_DIR / f"{sid}.jsonl").open("a", encoding="utf-8") as f:
        f.write(json.dumps(row, ensure_ascii=False) + "\n")


def handle_session_start(p: dict) -> None:
    sid = p.get("session_id")
    if not sid:
        return
    emit(sid, {"ts": now_iso(), "hook": "SessionStart",
               "source": p.get("source", ""), "cwd": p.get("cwd", ""),
               "detail": "epoch initialized"})


def handle_post_tool_use(p: dict) -> None:
    sid = p.get("session_id")
    tool = p.get("tool_name", "")
    if not sid or tool not in JOURNAL_TOOLS:
        return
    ti = p.get("tool_input") or {}
    tr = p.get("tool_response") or {}
    if tool == "Bash":
        cmd = (ti.get("command") or "").strip()
        cat, path, detail = classify_bash(cmd), "", cmd[:200]
    else:
        raw = ti.get("file_path") or ti.get("notebook_path") or ""
        cat = classify_edit(raw) if raw else "other"
        path, detail = relpath(raw), ""
    ok = not (isinstance(tr, dict) and (tr.get("is_error") or tr.get("interrupted")))
    emit(sid, {"ts": now_iso(), "hook": "PostToolUse", "tool": tool,
               "cat": cat, "path": path, "detail": detail, "ok": ok})


def handle_stop(p: dict) -> None:
    sid = p.get("session_id")
    if not sid:
        return
    dest = JOURNAL_DIR / f"{sid}.jsonl"
    if not dest.exists():
        return
    counts: Counter[str] = Counter()
    fails = 0
    for line in dest.read_text(encoding="utf-8").splitlines():
        if not line:
            continue
        try:
            row = json.loads(line)
        except json.JSONDecodeError:
            continue  # tolerate partial writes from prior crash
        if row.get("hook") != "PostToolUse":
            continue
        counts[row.get("cat", "other")] += 1
        if row.get("ok") is False:
            fails += 1
    emit(sid, {"ts": now_iso(), "hook": "Stop",
               "summary": dict(counts), "total": sum(counts.values()),
               "fails": fails, "detail": "epoch finalized"})


HANDLERS = {
    "SessionStart": handle_session_start,
    "PostToolUse": handle_post_tool_use,
    "Stop": handle_stop,
}


def main() -> int:
    if len(sys.argv) < 2 or sys.argv[1] not in HANDLERS:
        print(f"journal.py: invalid hook phase {sys.argv[1:]!r}", file=sys.stderr)
        return 2
    raw = sys.stdin.read()
    HANDLERS[sys.argv[1]](json.loads(raw) if raw.strip() else {})
    sys.stdout.write('{"continue": true}\n')
    return 0


if __name__ == "__main__":
    sys.exit(main())
