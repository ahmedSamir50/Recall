# Git hooks

Run once per clone:

```bash
git config core.hooksPath .githooks
```

`prepare-commit-msg` removes `Co-authored-by: Cursor <cursoragent@cursor.com>` if an editor or tool adds it.
