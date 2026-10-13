# Development rules

These apply to every plan and every code change. A plan that violates one is a push-back from the owning expert, even when InitDocs or the user asked for the violation. Say which rule broke and what to do instead.

| Rule | Meaning | Enforced by |
|---|---|---|
| Single source of truth | No magic strings. Protocols, tiers, modalities, prefixes, paths keys, and error codes live in an enum or one static class. User-facing sentences do **not** live in those consts. A literal is one of three things: a canon constant, an `appsettings` key, or a resx key. ONNX graph input and output names are canon constants, because a settings change cannot retarget a compiled graph. | `dotnet-architect` |
| Plan grain | A plan names every file to add, edit, or move, and the folder that file belongs in. It names each literal's home. It includes the Arabic and English sentence a person will read. A later agent that needs a file, folder, or sentence the plan did not name stops and amends the plan. A benchmark that returns a constant is not a measurement. | `dotnet-architect`, `unit-tests` |
| User-facing copy | Initially Arabic and English only (`ar`, `en`). Every string a person can see is in both `.resx` files, including empty states and errors. Arabic layout is RTL. | `ux-ui` |
| Abstractions first | Port in `Recall.Core` before the MAUI, ONNX, ZVec, or SQLite adapter. UI depends on the port, not the adapter. | `dotnet-architect` |
| Clean architecture | Dependencies point inward. Core has no MAUI, no Razor, no HTTP. UI has no native interop. Adapters sit in the MAUI head. Do not add empty layer projects for their own sake. | `dotnet-architect` |
| No duplication | One implementation of a rule (id scheme, tier policy, prefix selection, scope overlap). A second copy is a push-back. | `dotnet-architect` |
| No god types | A class, method, or Razor page that mixes capture, embedding, indexing, query, and UI is a push-back. Split by use case. | `dotnet-architect`, `ux-ui` |
| KISS, YAGNI, SOLID | Small types, one reason to change, depend on abstractions. Extract a shared helper on the second real use. A platform port is an abstraction even with one implementation per RID. Do not build a framework for a single call site. | `dotnet-architect` |
| Known patterns | Use a named pattern when the problem is the one that pattern exists for (strategy per RID, factory for encoder mode, decorator for timing, options for policy). Do not name a pattern and then invent a cousin. | `dotnet-architect` |
| Reusability first | Shared policy lives in Core. Platform heads implement ports; they do not re-decide tier widths or prefix strings. | `dotnet-architect` |
| No memory leaks | Dispose ONNX sessions, ZVec handles, watchers, and SQLite connections. Unsubscribe event handlers. Bound the ingest channel. One `HttpClient` via `IHttpClientFactory` for model download. | `performance`, `maui-platform` |
| Runtime cost | Stay inside the budgets in the performance skill. No LINQ or allocation on the query hot path that the budget cannot afford. | `performance` |
| Memory cost | Do not keep text, vision, and audio encoders loaded together. Do not store a vector at 128 dimensions or below. Whether old items shrink later is undecided. Do not cache unbounded query results. | `performance`, `vectors-embeddings` |
| SQL cost | `checkpoints.db` uses parameterized SQL, a transaction around cursor updates, and indexes that match the lookup (path, file id). No string-concatenated SQL. ZVec is not SQL; do not "optimize" HNSW with SQL hints. | `performance` |
| Test honesty | See [test-honesty.md](test-honesty.md). The verdict is honest tests of critical paths, hot paths, and edge cases. 60% at M1 and 70% later stay as a floor. The floor is not the verdict and not the homework. | `unit-tests`, `retrieval-qa` |

## Copy versus retrieval languages

User-facing locales start at `ar` and `en`. That decision replaces the InitDocs store-localization list (EN, AR, ZH, HI, FR) for UI chrome.

The retrieval golden set in InitDocs is still EN, AR, ZH, HI, FR. That is a different decision. A plan must not collapse the two lists, and must not drop a golden language, without an explicit user decision. Flag the split.

## What "push back" means here

If a rule and a doc sentence disagree, the rule wins for new work and the doc sentence is an amendment. If two rules seem to disagree (reuse versus YAGNI), follow the table: platform ports exist up front; other helpers wait for a second use. Write that reasoning in the sign-off. Do not pick the slogan that matches the user's wording.
