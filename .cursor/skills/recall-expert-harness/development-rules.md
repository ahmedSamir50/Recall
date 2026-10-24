# Development rules

These apply to every plan and every code change. A plan that violates one is a push-back from the owning expert, even when InitDocs or the user asked for the violation. Say which rule broke and what to do instead.

| Rule | Meaning | Enforced by |
|---|---|---|
| Single source of truth | No magic strings. Protocols, tiers, modalities, prefixes, paths keys, and error codes live in an enum or one static class. User-facing sentences do **not** live in those consts. A literal is one of three things: a canon constant, an `appsettings` key, or a resx key. ONNX graph input and output names are canon constants, because a settings change cannot retarget a compiled graph. | `dotnet-architect` |
| Plan grain | A plan is an execution manifest. Every file path, every type and method signature, every branch including the failure, every literal's home, and the Arabic and English sentence are written out. Every test names the method, the branch, and the assertion. A later agent that needs a file, signature, branch, sentence, loop order, exception type, or assertion value the manifest did not write stops and names the missing line. It does not invent one. A code review that would rewrite most of the touched files stops and amends the plan. A benchmark that returns a constant is not a measurement. | `dotnet-architect`, `unit-tests` |
| Plan progress | After an accepted `@recall:` plan is executed, recompute the one progress table at the end of `docs/plan.md` in that same turn. Phase rows, epic rows under them, task rows under each epic, and a final all-epics row. A task is 100, 50, or 0. Epic, phase, and the footer are the mean of their tasks, rounded to the nearest whole number. No second progress table. | `docs-mkdocs` |
| Docs follow the change | A @recall: plan lists the docs pages the change creates or updates. Execution writes them in that turn. The user does not have to ask. Unbuilt behavior is not written as current. A new heading under docs/code that quotes the branch is not that page. | `docs-mkdocs` |
| Close means done | When the user asks to close a task, epic, or phase, that plan finishes every remaining child. After execution the parent is `done` and 100. A leftover child requires a named blocker, external dependency, or unaccepted push-back in the sign-off. Another plan for the same close is a failed execution. | `dotnet-architect`, `docs-mkdocs` |
| User-facing copy | Initially Arabic and English only (`ar`, `en`). Every string a person can see is in both `.resx` files, including empty states and errors. Arabic layout is RTL. | `ux-ui` |
| Abstractions first | Port in `Recall.Core` before the MAUI, ONNX, ZVec, or SQLite adapter. UI depends on the port, not the adapter. | `dotnet-architect` |
| Clean architecture | Dependencies point inward. Core has no MAUI, no Razor, no HTTP. UI has no native interop. Adapters sit in the MAUI head. Do not add empty layer projects for their own sake. | `dotnet-architect` |
| No duplication | One implementation of a rule (id scheme, tier policy, prefix selection, scope overlap). A second copy is a push-back. | `dotnet-architect` |
| No god types | A class, method, or Razor page that mixes capture, embedding, indexing, query, and UI is a push-back. Split by use case. | `dotnet-architect`, `ux-ui` |
| KISS, YAGNI, SOLID | Small types, one reason to change, depend on abstractions. Extract a shared helper on the second real use. A platform port is an abstraction even with one implementation per RID. Do not build a framework for a single call site. | `dotnet-architect` |
| Known patterns | Use a named pattern when the problem is the one that pattern exists for (strategy per RID, factory for encoder mode, decorator for timing, options for policy). Do not name a pattern and then invent a cousin. | `dotnet-architect` |
| Reusability first | Shared policy lives in Core. Platform heads implement ports; they do not re-decide tier widths or prefix strings. | `dotnet-architect` |
| No memory leaks | Dispose ONNX sessions, ZVec handles, watchers, and SQLite connections. Unsubscribe event handlers. Bound the ingest channel. One `HttpClient` via `IHttpClientFactory` for model download. | `performance`, `maui-platform` |
| Embedder runtime | Text embeddings run in-process through llama.cpp tag b11455. Do not add a helper process. Do not add LLamaSharp 0.30.0. OnnxTextEmbeddingEncoder, GemmaTextTokenizer, and Tokenizers.DotNet are not the text path. | `dotnet-architect` and `maui-platform` |
| Runtime cost | Stay inside the budgets in the performance skill. No LINQ or allocation on the query hot path that the budget cannot afford. | `performance` |
| Memory cost | Do not keep text, vision, and audio encoders loaded together. Do not store a vector at 128 dimensions or below. Whether old items shrink later is undecided. Do not cache unbounded query results. | `performance`, `vectors-embeddings` |
| SQL cost | `checkpoints.db` uses parameterized SQL, a transaction around cursor updates, and indexes that match the lookup (path, file id). No string-concatenated SQL. ZVec is not SQL; do not "optimize" HNSW with SQL hints. | `performance` |
| Test honesty | See [test-honesty.md](test-honesty.md). The verdict is honest tests of critical paths, hot paths, and edge cases. 60% at M1 and 70% later stay as a floor. The floor is not the verdict and not the homework. | `unit-tests`, `retrieval-qa` |
| Git authorship | Never add a `Co-authored-by` trailer to a commit message. That includes `Co-authored-by: Cursor <cursoragent@cursor.com>` and any other agent or tool identity. Never run `git commit` with `--trailer`; use `git commit -m "summary"` only. Cursor may inject a trailer automatically—`.cursor/hooks.json` blocks that shell pattern and `.githooks/prepare-commit-msg` strips it when `core.hooksPath` is `.githooks`. Commits list only the human author the user expects on `main`. | `dotnet-architect`, `docs-mkdocs` |

## Copy versus retrieval languages

User-facing locales start at `ar` and `en`. That decision replaces the InitDocs store-localization list (EN, AR, ZH, HI, FR) for UI chrome.

The retrieval golden set in InitDocs is still EN, AR, ZH, HI, FR. That is a different decision. A plan must not collapse the two lists, and must not drop a golden language, without an explicit user decision. Flag the split.

## What "push back" means here

If a rule and a doc sentence disagree, the rule wins for new work and the doc sentence is an amendment. If two rules seem to disagree (reuse versus YAGNI), follow the table: platform ports exist up front; other helpers wait for a second use. Write that reasoning in the sign-off. Do not pick the slogan that matches the user's wording.
