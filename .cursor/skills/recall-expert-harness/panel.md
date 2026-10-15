# Expert panel

Each expert is a Cursor agent (`.cursor/agents/<id>.md`) plus a skill (`.cursor/skills/<id>/SKILL.md`). The skill is the checklist. The agent is the persona. `canon.md` wins if they disagree.

The panel is the ids in the table below. Do not search for an expert who is not listed. The first line of `## Expert sign-offs` names one or more of those ids after `Direct:`, from the rows that plan changes. An id not on that line is advisory. Direct and advisory each check their own row once.

| Id | Owns | Call in when the work touches |
|---|---|---|
| `dotnet-architect` | Process boundaries, solution layout, DI, .NET 10, analyzers, ADRs | Projects, hosting, composition root, a new dependency, an architecture change |
| `vectors-embeddings` | ONNX sessions, MRL tiers, ZVec collections, HNSW / RaBitQ, RRF | Embeddings, indexes, insert or query paths, dimension or shard changes |
| `nlp` | Task prefixes, tokenization, chunking, BM25 text, multilingual behavior, deterministic labels | Query text, languages, OCR/ASR assumptions, cluster labels |
| `maui-platform` | Four RIDs, filesystem scope, foreground iOS scan, video sampling, model download, secure storage | Platform heads, permissions, battery, store constraints |
| `ux-ui` | Razor UX, onboarding, result cards, empty states, localization, RTL | Screens, copy, accessibility, Blazor components |
| `privacy-local-first` | Egress ban, sync-ready schema without sync code, threat model | Network, logging, telemetry, crypto, identifiers, file paths |
| `performance` | Latency, RSS, disk, battery, BenchmarkDotNet gates | Budgets, hot paths, model residency, CI perf numbers |
| `unit-tests` | xUnit unit and property tests for pure logic | `tests/Recall.UnitTests`, testable seams, fakes |
| `retrieval-qa` | Golden corpus, recall@k, integration fixtures, crash recovery, egress tests | `Recall.CorpusTests`, `Recall.IntegrationTests`, corpus builder |
| `docs-mkdocs` | Latest App Docs information architecture, ADR hygiene, MkDocs | `docs/`, nav, review write-ups, keeping InitDocs labeled as history |

## Who reviews what

Use this when hardening a document. Primary writes the finding. Consulted experts comment only on their rows.

| Document concern | Primary | Consult |
|---|---|---|
| Goals, non-goals, anti-goals, milestone exit | `dotnet-architect` | `privacy-local-first`, `performance` |
| Solution layout, DI, analyzers, branching | `dotnet-architect` | `unit-tests` |
| Encoder loading, MRL, shards, HNSW, RRF, query/insert pipelines | `vectors-embeddings` | `nlp`, `performance` |
| Prefixes, 8K context, languages, labeler, BM25 payload | `nlp` | `vectors-embeddings`, `ux-ui` |
| Platform matrix, watchers, iOS scan, RIDs | `maui-platform` | `ux-ui`, `performance` |
| Screens, onboarding, copy, localization | `ux-ui` | `privacy-local-first`, `nlp` |
| Egress, telemetry, sync design, identifiers | `privacy-local-first` | `dotnet-architect` |
| Perf tables, memory, battery | `performance` | `vectors-embeddings`, `maui-platform` |
| Unit and property tests | `unit-tests` | `vectors-embeddings` |
| Golden queries, corpus, crash and egress tests | `retrieval-qa` | `nlp`, `privacy-local-first` |
| Doc structure, MkDocs, ADR index, stale-passage sweep | `docs-mkdocs` | `dotnet-architect` |

`@recall:` and `@reacall:` sign every expert in this chat, using [planning-chain.md](planning-chain.md). One read of the shared files. Each expert returns a [sign-off](sign-off.md). Do not launch expert subagents. Do not sign the whole panel for a local edit. For a doc hardening pass that is not a plan, apply the primary plus the consulted column in this chat. Apply every row for a full pass over Latest App Docs.

## Out of panel on purpose

No separate LLM expert: an LLM sidecar is an anti-goal. No web-client expert: there is no web target. No sync-implementation expert: v1 ships sync disabled; `privacy-local-first` guards the design so it is not built early and not broken. IdeaTank siblings (PhotoMind, LegalLens, and the rest) are not separate v1 apps. Granted source files are text in v1.
