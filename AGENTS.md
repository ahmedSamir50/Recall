# Recall

On-device multimodal second brain. One .NET 10 MAUI Blazor Hybrid app, EmbeddingGemma 2 through in-process llama.cpp, ZVec.NET in that same process. The class in the tree is LlamaTextEmbeddingEncoder. v1 is local-only.

`InitDocs/` is the initial idea, not the final specification. Experts and the review harness live under `.cursor/`. Start at `.cursor/skills/recall-expert-harness/SKILL.md`. The working canon, including known stale passages in InitDocs, is `.cursor/skills/recall-expert-harness/canon.md`.

`@recall: <plan>` or `@reacall: <plan>` runs the full expert panel in that same chat before a plan is accepted. Each expert signs off with evidence, a verdict, confidence, options, alternatives, and risks. Do not spawn a subagent per expert. Development rules and test honesty live next to that skill.

Do not implement device sync, a web client, a chat UI, a helper process, or ambient OS capture unless a later spec the user has accepted says so.

Git: never `git commit --trailer` or any `Co-authored-by` line. Use `git commit -m "summary"` only. `.githooks` strips injected Cursor co-author lines; do not add bash hook scripts on Windows.
