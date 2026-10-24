#!/usr/bin/env bash
# Deny git commits that add Cursor (or any) Co-authored-by trailers.
set -euo pipefail
input=$(cat)
command=""
if command -v python >/dev/null 2>&1; then
  command=$(printf '%s' "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('command',''))" 2>/dev/null || true)
fi
if [ -z "$command" ] && command -v jq >/dev/null 2>&1; then
  command=$(printf '%s' "$input" | jq -r '.command // empty' 2>/dev/null || true)
fi
if [ -z "$command" ]; then
  command="$input"
fi

lower=$(printf '%s' "$command" | tr '[:upper:]' '[:lower:]')
if printf '%s' "$lower" | grep -qE 'git[[:space:]]+commit'; then
  if printf '%s' "$lower" | grep -qE '(--trailer|co-authored-by:|cursoragent@cursor\.com)'; then
    printf '%s\n' '{"permission":"deny","user_message":"Recall forbids Co-authored-by: Cursor on commits.","agent_message":"Use git commit -m \"summary\" only. Never pass --trailer or Co-authored-by. See development-rules.md Git authorship."}'
    exit 0
  fi
fi

printf '%s\n' '{"permission":"allow"}'
exit 0
