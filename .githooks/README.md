# Git hooks

Run once per clone:

```bash
git config core.hooksPath .githooks
```

`prepare-commit-msg` and `commit-msg` call hidden PowerShell once per commit and remove `Co-authored-by: Cursor <cursoragent@cursor.com>` if Cursor adds it. There is no Cursor `beforeShellExecution` hook on `git commit` (that blocked every agent commit and spawned extra shells).
