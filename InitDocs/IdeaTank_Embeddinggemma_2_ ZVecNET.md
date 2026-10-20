> Historical seed. The accepted text-embedder direction is in-process llama.cpp (ADR 0012). This file is not the spec.

# IdeaTank — Building on `google/embeddinggemma-2` + `ZVec.NET`

Historical seed. EmbeddingGemma sentences in this file that say ONNX are not the spec. The accepted embedder is in-process llama.cpp. PP-OCRv6 stays ONNX.

> A two-round brainstorm of super-applications, products, and POCs that become possible — and in some cases *only* possible — when you pair **Google's EmbeddingGemma 2** with **ZVec.NET** (the .NET SDK for Alibaba's in-process ZVec vector database).
>
> **Round 1** captured the first-pass ideas. **Round 2** refines them after verifying the actual capabilities of both pieces of the stack, and adds new ideas unlocked by features I had under-exploited the first time (MRL truncation, task prefixes, modular encoder loading, 8K context, ONNX export, Native AOT, mobile RIDs, in-DB hybrid rerankers).
>
> Author's note: the chat-style "RAG-with-an-LLM-side-car" idea (DropRAG) is **parked** per scope — the ideas below that involve retrieval deliberately stay in the *search-only* lane so they ship without an LLM dependency.

---

## 0. TL;DR

The thesis of this document is that the **EmbeddingGemma 2 + ZVec.NET** pairing is uniquely suited to a class of applications that hosted vector databases and server-side embedders structurally cannot serve: **private, offline, multimodal, multilingual, low-latency retrieval that runs entirely inside a .NET process — on a phone, on a laptop, on an industrial edge box, or inside a Unity editor**.

The flagship application is **`Recall`**, an on-device multimodal "second brain" that captures text, images, audio, and video from your life and lets you query them in any language, in any modality, with sub-millisecond latency, completely offline, with end-to-end-encrypted peer-to-peer sync between your devices. No server ever sees a vector.

The POC portfolio is organized into three lanes (Web, App/Mobile, Edge) plus a deliberate **"do not use ZVec.NET for this"** lane to show taste. Round 2 adds ~10 refined ideas that exploit the capabilities I had under-weighted in round 1.

---

## 1. Verified Capabilities of the Stack

Before brainstorming, I verified the actual specs of both components. This matters: round 1 leaned on plausible assumptions, and several of them turned out to be conservative.

### 1.1 `google/embeddinggemma-2` — confirmed facts

Source: Hugging Face model card, Google DeepMind developer guide, Ollama library entry.

| Property | Value |
|---|---|
| **Builder** | Google DeepMind |
| **License** | Apache 2.0 |
| **Built on** | Gemma 4 decoder architecture |
| **Total parameters** | 740M (modular) |
| **Text encoder** | 270M (130M transformer + 140M embedder) |
| **Vision encoder** | 170M (selectively loadable) |
| **Audio encoder** | 300M (selectively loadable) |
| **Architecture** | 24 layers, model dim 512, hidden dim 2048, sliding window 1024 |
| **Attention** | GQA / MQA, 4 heads, 2/1 KV-heads (local/global), 5:1 local:global ratio |
| **Vocabulary** | 262,144 |
| **Pooling** | Mean pooling |
| **Projection** | 512 → 768 |
| **Output dimensionality** | 768 (native) |
| **Context window** | 8,192 tokens (≈ minutes of audio/video per embedding) |
| **Supported modalities** | Text, Code, Images, Video, Audio — in one shared 768-d space |
| **Languages** | 100+ |
| **Code performance** | ~+14% over EmbeddingGemma 1 on MTEB-code |
| **Matryoshka Representation Learning (MRL)** | Native truncation to 128d / 256d / 512d / 768d, up to **6× storage reduction** with minimal quality loss |
| **Task-instruction prefixes** | Lightweight text prefixes steer the same model for *search*, *classification*, *clustering*, *semantic similarity* — without re-training |
| **Modular encoder loading** | Text-only (270M), text+vision (440M), full multimodal (740M) — pay only for what you use |
| **ONNX export** | Official `onnx-community/embeddinggemma-2-ONNX` — fits the .NET ONNX Runtime ecosystem perfectly |
| **Benchmark highlights** | MTEB multilingual 61.36 · MTEB code 78.68 · MIEB-lite 64.64 · MMEB-v2 Image 57.28 · MMEB-v2 Video 50.67 · MSEB audio retrieval 69.54 |

The five capabilities I most under-exploited in round 1 were: **MRL truncation**, **task-instruction prefixes**, **modular encoder loading**, **8K context (minutes of audio/video per embedding)**, and the existence of an official **ONNX export**. Each of them unlocks specific product ideas that were not obvious before.

### 1.2 `AdamSystems.ZVec.NET` — confirmed facts

Source: GitHub README, NuGet package metadata, docs site.

| Property | Value |
|---|---|
| **What it is** | Production .NET SDK for Alibaba ZVec (`github.com/alibaba/zvec`) |
| **Status** | Beta `1.0.0-beta.6+zvec.0.7.0`, Apache 2.0 |
| **Target frameworks** | `net8.0`, `net9.0`, `net10.0` |
| **Native AOT** | Compatible — publish single-file trimmed binaries without IL warnings |
| **DI integration** | `AddZVec()` / `AddZVecCollection()` for ASP.NET Core, MAUI, Blazor Server |
| **Typed ODM** | `ZVec.NET.Mapping` — POCO mapping, expression filters, typed CRUD/DDL |
| **Sync + Async APIs** | Sync for batch jobs, `ValueTask` + `CancellationToken` for ASP.NET Core |
| **Vector pipelines** | Pin-based — `ReadOnlyMemory<float>` on hot paths, no `float[]` copies |
| **Native lifecycle** | `SafeZvecHandle`, `Dispose`/`Destroy`/`Shutdown` semantics |
| **Index types supported** | HNSW, Flat, IVF, **HNSW-RaBitQ** (quantized, x86_64 + AVX2 only), **DiskANN** (Linux-only, libaio optional), **Vamana** (static-dataset optimized), Invert, **FTS** (full-text search) |
| **Hybrid search** | Built-in — dense + sparse + scalar filter + FTS in one query |
| **In-DB rerankers** | RRF (Reciprocal Rank Fusion) and Weighted — no external reranker model needed |
| **Schema evolution** | Supported in-DB |
| **Shipped native RIDs** | `win-x64`, `win-arm64` (pending MSVC fix), `linux-x64`, `linux-arm64`, `osx-arm64`, `osx-x64`, `android-arm64`, `android-x64`, `ios-arm64`, `iossimulator-arm64`, `maccatalyst-arm64` (soft) |
| **Not supported** | Blazor WebAssembly (no native RID) |
| **Samples shipped** | ASP.NET Minimal API, **MAUI Blazor Hybrid (offline/edge RAG)**, Console |
| **Docs** | GitHub Pages wiki |

The five capabilities I most under-exploited in round 1 were: **Native AOT** (single-binary serverless / edge deployment), the existing **MAUI Blazor Hybrid sample** (proven mobile-RAG path), the **multiple index types beyond HNSW** (DiskANN for billion-scale-on-disk, Vamana for static archives, RaBitQ for quantized memory savings), the **in-DB RRF / Weighted rerankers** (no external reranker call needed → cheaper hybrid search), and the breadth of **mobile RIDs** (true iOS + Android + macOS Catalyst offline support).

### 1.3 Alibaba ZVec — the underlying DB

Source: `github.com/alibaba/zvec`, `zvec.org`.

ZVec is an open-source, in-process vector database — described as "the SQLite of vector databases". It runs entirely inside your application process: no server, no daemon, no network calls. Battle-tested inside Alibaba. It provides persistent storage, schema management and evolution, scalar filtering, crash recovery, dense + sparse vector search, full-text retrieval, and hybrid search.

This is the architectural property that makes the entire idea portfolio defensible: **no network round-trip, no egress cost, no PII ever leaves the device**. Hosted vector databases cannot compete on those axes by design.

---

## 2. Strategic Insight — Why This Pairing Is a Moat

Most vector-database companies are fighting a server-side war (latency, scale, cost-per-query). Most embedding-model releases are chasing MTEB scores. This pairing is rare because it owns both halves of a stack that collapses entirely onto the device:

- **No egress cost** — queries never leave the device, because both the embedder and the DB live in-process.
- **No cold-start penalty** — the DB is in your process memory; the embedder is a 270M–740M ONNX model that loads in milliseconds.
- **PII never touches a cloud** — unlocks regulated verticals (health, legal, finance, defense, personal) that hosted vector DBs literally cannot sell into.
- **Works offline** — on a plane, in a basement, on cheap Android in emerging markets, in a hospital with an air-gapped policy.
- **Native AOT** — ZVec.NET binaries can ship as single trimmed executables with millisecond startup, perfect for serverless / edge / IoT.
- **Mobile RIDs** — true iOS + Android + macOS Catalyst offline vector search is real today, not aspirational.
- **In-DB rerankers** — hybrid (BM25 + dense) retrieval without an external reranker model means one fewer moving part and lower latency.
- **MRL truncation × in-process DB** — store 128d vectors for 6× the data in the same RAM budget; on a phone with 4GB free, that's the difference between "10k photos" and "60k photos" of recall.

So the winning ideas are **not** "a better RAG-as-a-service." They are the ones that are *impossible* when you depend on a remote vector DB plus a hosted embedder. Every idea in this document is filtered through that lens.

---

## 3. The Super Application — `Recall`

`Recall` is the application that *only* this stack can build well, and it is the one I'd actually ship first.

### 3.1 The pitch

`Recall` is an on-device, multimodal, multilingual "second brain" that runs across Web + Mobile + Edge as one product, not three. It captures text, images, audio snippets, video frames, and screen states from your life, embeds them locally with EmbeddingGemma 2, indexes them in ZVec.NET, and lets you query them in any language, in any modality, with sub-millisecond recall — completely offline, with end-to-end-encrypted peer-to-peer sync between your devices.

### 3.2 The architecture, concretely

- **Edge capture layer** — a watch / glasses / phone background agent that grabs frames, audio snippets, screen states, and your notes.
- **Embedding layer** — EmbeddingGemma 2 via ONNX Runtime, loaded with **modular encoders**: text-only (270M) for fast background indexing of text streams, full multimodal (740M) for user queries and cross-modal capture.
- **Indexing layer** — ZVec.NET inside the app process.
  - **HNSW-RaBitQ** on x64 desktop (quantized, memory-efficient).
  - Plain **HNSW** on ARM mobile.
  - **FTS** for exact-match recall (file names, tags, exact phrases).
  - **In-DB RRF reranker** fuses dense + FTS in one query — no external reranker call.
- **MRL tiering** — recent / hot data stored at full 768d for max recall; archival data stored at 256d or 128d for 3–6× storage density. The same model produces both, just truncated.
- **Task prefixes** — the same corpus is queried with different instruction prefixes: `"search"` for user recall queries, `"cluster"` for nightly auto-organization, `"classify"` for auto-tagging. One model, three behaviors.
- **8K context** — entire meeting recordings, long articles, and minutes-long voice memos are embedded as single vectors, not chunked and averaged.
- **Sync** — peer-to-peer (or end-to-end-encrypted CRDT) between your devices. No central server ever sees a vector.
- **Deployment targets** —
  - Mobile: **MAUI Blazor Hybrid** (the existing sample proves this works) targeting `android-arm64` and `ios-arm64` RIDs.
  - Desktop: **Native AOT** single-binary for Windows / macOS / Linux.
  - Web dashboard: ASP.NET Minimal API + Blazor Server for in-process admin UI (no Blazor WASM since ZVec.NET has no WASM RID — the dashboard talks to a local in-process DB).

### 3.3 The killer demo

*"The red scooter I saw near the Colosseum last summer"* — typed or spoken in Arabic, English, or Hindi — returns the exact photo frame, the surrounding 30 seconds of audio, and the note you scribbled about it, in under 50 ms, on a mid-tier Android, completely offline. Hosted vector DBs are blocked by privacy + latency + cost; on-device-only embedders without a real DB can't do million-vector recall. Only this stack can.

### 3.4 Why this is the super app and not just another POC

`Recall` exercises every differentiating capability of both halves of the stack simultaneously — multimodality, multilinguality, MRL, task prefixes, 8K context, ONNX, in-process DB, mobile RIDs, hybrid search, in-DB rerankers, Native AOT. If `Recall` works, the stack works for everything else in this document. If it doesn't, you've found the limit of the stack cheaply.

---

## 4. Round 1 POC Portfolio (kept, with status notes)

These are the ideas from the first brainstorm. They are kept verbatim, with a status note appended to each based on what the verified capabilities revealed.

### 4.1 Web

#### 4.1.1 `PrivateLens` — browser-side semantic search over your own browsing life
History, bookmarks, open tabs, downloaded PDFs, and clipped screenshots all embedded and indexed in-browser. Zero upload. Sell to journalists, researchers, lawyers.

> **Status:** Adjust. ZVec.NET does **not** ship a Blazor WebAssembly RID, so a pure in-browser version is not viable today. The right shape is a **local desktop daemon** (Native AOT binary, runs on the user's machine) that the browser extension talks to over localhost. The daemon embeds via ONNX Runtime and queries ZVec.NET in-process. Privacy is preserved because the daemon is local — the browser extension never sees the vectors. This is actually a stronger architecture than the original in-browser pitch because ONNX Runtime + ZVec.NET on a desktop CPU will out-perform any WebGPU-based in-browser stack by a wide margin.

#### 4.1.2 `DropRAG` — drag-and-drop confidential document Q&A
> **Status: Parked.** Per scope, this needs an LLM side-car to generate answers, and the user has explicitly parked LLM-dependent ideas. The *retrieval* half of DropRAG is still valuable and is preserved in `LegalLens` (Round 2 §5.1.3) as a pure-search variant.

#### 4.1.3 `ClipWeave` — multimodal web clipper
Clip text + images from any page in any language; later retrieve by meaning ("that chart about battery density").

> **Status:** Strengthen. EmbeddingGemma 2's native multimodality means images and text live in the *same* 768-d space — you can query "the chart about battery density" with text and match an image. Combined with MRL truncation (256d is plenty for clippings), this is now a much cheaper product to operate than I first thought.

### 4.2 App (mobile)

#### 4.2.1 `PhotoMind` — offline multilingual photo search
Photos embedded multimodally (image + OCR + EXIF caption) on-device. Multilingual queries.

> **Status:** Strengthen significantly. The MAUI Blazor Hybrid sample in ZVec.NET already proves the offline-RAG path on Android and iOS. With MRL truncation to 128d, the math now works for real photo libraries: a 50k-photo library at 128d × 4 bytes = 25 MB of vectors — trivially fits in mobile RAM. This is the recommended 30-day POC (see §8).

#### 4.2.2 `EchoNote` — voice-memo semantic recall
ASR → embed → index locally.

> **Status:** Strengthen dramatically. EmbeddingGemma 2's **8K context** means you can embed *minutes of audio per vector* — no chunking needed for typical voice memos. This was a real limitation in round 1 that I had not accounted for.

#### 4.2.3 `PolyglotPal` — personal interpreter with private translation memory
Builds a personalized TM of *your* vocabulary and domain.

> **Status:** Keep as-is. Still a strong idea; EmbeddingGemma 2's 100+ language coverage is the enabler.

### 4.3 Mobile / Edge

#### 4.3.1 `AmbientMemory` on wearables — always-on capture
> **Status:** Keep. This is `Recall` shrunk to a watch-face form factor. Ship after `Recall` core is stable.

#### 4.3.2 `EdgeMatch` — on-device multimodal product matching for industrial use
> **Status:** Strengthen. ZVec.NET's **Vamana** index is purpose-built for static catalogs (no updates) — a perfect fit for industrial product databases that change rarely. Combined with MRL 128d, a 100k-item catalog fits in ~50 MB.

#### 4.3.3 `HomeMind` — smart-home hub with multilingual mixed commands
> **Status:** Keep. Runs on a Raspberry Pi class edge box with ZVec.NET's `linux-arm64` RID.

### 4.4 Deliberately NOT using ZVec.NET (round 1)

Kept for taste. Round 2 adds to this list in §6.

- **Pure streaming classification / moderation** — a vector DB adds latency you don't need.
- **Cold generative chat with no retrieval** — if there's nothing to retrieve, the DB is dead weight.
- **Cross-tenant shared search** — hosted vector DB still wins here; partner instead.

---

## 5. Round 2 — Refined & New Ideas

After verifying the stack, the new ideas below exploit capabilities I had under-weighted in round 1: **MRL truncation**, **task prefixes**, **modular encoder loading**, **8K context**, **ONNX export**, **Native AOT**, **multiple index types**, **in-DB RRF / Weighted rerankers**, and the **MAUI Blazor Hybrid** sample path.

### 5.1 Web / Server

#### 5.1.1 `AOT-RAG Edge Function` (deployment pattern, not a product)
A single **Native AOT** binary that runs in AWS Lambda / Azure Functions consumption plan, embeds via ONNX Runtime, queries ZVec.NET in-process, and returns ranked results. Cold start under 100 ms because there is no JVM, no container pull, no IL JIT — just a trimmed binary that reuses a warm ZVec file. Sell as a **"RAG appliance"** that drops into any cloud account and bills per-execution. The same binary runs on a Raspberry Pi, so customers can prototype on edge and scale to cloud without rewriting.

#### 5.1.2 `Sigil` — task-steered personal search agent
EmbeddingGemma 2's instruction prefixes mean the same corpus can be queried in different "modes" without re-embedding. `Sigil` exposes three buttons to the user: **Find** (search prefix), **Group** (cluster prefix), **Tag** (classify prefix). Same vectors, three behaviors. Under the hood: ZVec.NET's HNSW for fast ANN, FTS for exact-match, RRF reranker to fuse them. Pure search, no LLM, no parked-RAG issue.

#### 5.1.3 `LegalLens` — multilingual legal document analyzer
EU and Asian legal documents frequently mix languages mid-page (EN + FR + DE, or EN + AR). EmbeddingGemma 2's 100+ language coverage handles this natively. ZVec.NET's **FTS + dense hybrid search with in-DB RRF** is the killer feature: the lawyer gets both the exact-citation match (BM25) *and* the semantic "what does this clause really mean" match (dense) in one ranked list, with no external reranker call. **Pure search — fits the parked-RAG constraint perfectly.**

#### 5.1.4 `CodeSense` — semantic code search across monorepos
EmbeddingGemma 2's +14% improvement on code tasks is the differentiator. ZVec.NET's FTS handles exact-symbol lookup (function names, class names, identifiers), and the dense vector handles "what does this function actually do" semantic queries. Hybrid RRF reranker fuses both. Multilingual codebases (mixed-language comments, docstrings in EN/AR/ZH/RU) work natively. This is the rare dev tool that can ship as a desktop app — no cloud dependency, no token-billing, no exfiltration of proprietary code.

### 5.2 App (mobile, via MAUI + ZVec.NET mobile RIDs)

#### 5.2.1 `Recall.MAUI` (the super app, scoped to a 30-day POC)
See §3. The 30-day POC version: MAUI Blazor Hybrid + ZVec.NET (android-arm64 RID) + embeddinggemma-2 ONNX Runtime + MRL 256d + text-only encoder mode for ingestion, full multimodal mode for query.

#### 5.2.2 `PolyFind` — cross-modal multilingual museum / collection explorer
Point the phone at any artifact, ask in any language, get related items. Uses vision + audio (curator narration) + text (label) in one 768-d space. Built on MAUI + ZVec.NET's HNSW + EmbeddingGemma 2's multimodal encoders. This is a beautiful, demoable POC because every museum curator instantly gets it.

#### 5.2.3 `FieldGuide` — offline field biology / agriculture
Point the phone at a plant, pest, or soil sample; embed image + audio (bird call) + GPS tag; match against an on-device multimodal catalog. Works in remote fields without signal. MAUI + ZVec.NET + ONNX Runtime, Vamana index (the catalog is static). Multilingual labels handled natively, which matters for agricultural extension work in multilingual regions.

#### 5.2.4 `Mirror` — personal reflection / journaling app
Embeds journal entries (text), voice notes (audio), photos (image). Ask "when did I last feel like this?" — emotional semantic search across modalities. Mental-health / coaching vertical. Pure search, no LLM. The 8K context means a whole journal entry fits in one embedding.

#### 5.2.5 `PolyglotTM` — translation memory engine for translators
EmbeddingGemma 2 multilingual + ZVec.NET FTS for exact TM hits + dense for fuzzy semantic matches. Beats traditional TM tools that only do string matching. A translator's existing TM (often 100k+ segment pairs) becomes a semantic search corpus overnight.

### 5.3 Edge / Industrial / Embedded

#### 5.3.1 `Vamana Archive` — static multimodal archives (museums, libraries, film studios)
Vamana is purpose-built for static datasets that are built once and queried forever. Pair with MRL 128d (good enough for archival recall) and EmbeddingGemma 2's multimodal embeddings (catalog cards + images + audio narration in one space). Once built, the index never needs re-building — Vamana's static-build optimization makes it faster than HNSW for this workload.

#### 5.3.2 `DiskANN Vault` — billion-scale on-disk retrieval for enterprises
DiskANN in ZVec.NET is Linux-only, but it's designed for billion-vector datasets that don't fit in RAM — exactly the scale of enterprise knowledge bases. Pair with Native AOT single-binary deployment. The pitch: "Your entire corporate corpus, searchable in milliseconds, on a single Linux box with an SSD." No cluster, no Kubernetes, no Pinecone bill.

#### 5.3.3 `MedSearch` — HIPAA-compliant in-process clinical search
Embeds patient notes (text), scans (image), ECG strips (image), and dictations (audio) multimodally. **Never leaves the hospital machine** — ZVec.NET's in-process architecture is the only design that satisfies the most restrictive hospital IT policies (air-gapped, no cloud APIs, no PII egress). SafeHandles give the lifecycle discipline that medical software requires.

#### 5.3.4 `RetailRover` — retail associate handheld terminal
Photograph a product or part, match against an on-device catalog. No network needed in basements / warehouses. Multilingual labels handled natively. Built on MAUI + ZVec.NET mobile RID + Vamana (catalog is static) + EmbeddingGemma 2 multimodal.

#### 5.3.5 `HarmonyEngine` — music sample library management
EmbeddingGemma 2's audio encoder + ZVec.NET hybrid search. "Find me a kick drum that sounds like this one but darker." Multimodal: pair the audio with the album art and the genre tag. The 8K context means whole song sections can be embedded as single vectors.

#### 5.3.6 `Cortex` — on-device AI for aviation / pilots
Embeds pre-flight briefings (text), runway diagrams (image), and ATC audio (audio). Instant offline recall in flight, where connectivity is unreliable and latency is unacceptable. Pure search — no LLM dependency that could fail mid-flight.

### 5.4 Game Development (the .NET angle I missed in round 1)

#### 5.4.1 `Unity AssetMind` — multimodal asset library inside the Unity editor
Unity uses C# and .NET; ZVec.NET runs in-process inside the editor. Embed every art asset (texture, mesh preview, audio clip, code snippet) and search by meaning: "the enemy sound that matches this character concept art". Audio + image + text in one space, fused with FTS for exact asset-name lookup.

#### 5.4.2 `Godot Mind` — same concept for Godot (which now supports .NET 8+)
Multimodal asset recall inside the Godot editor. Same architecture, different host.

### 5.5 Creator / Knowledge Worker

#### 5.5.1 `Synapse` — knowledge worker dashboard
ASP.NET + Blazor Server + ZVec.NET in-process. Ingests your emails, docs, chats, code, screenshots. Multimodal recall across all of it. Runs as a desktop app — no server needed because ZVec.NET is in-process. The pitch to consultants and lawyers: "Your entire work product, searchable in 50 ms, on your laptop, with nothing leaving your machine."

#### 5.5.2 `MirrorDesk` — personal analytics dashboard for creators
Embeds YouTube videos (audio + visual), blog posts (text), tweets. "Which of my videos talks about X?" returns the exact video and timestamp. MRL truncation keeps it cheap enough to run on a creator's laptop.

#### 5.5.3 `InsightVault` — academic literature management
Multimodal (figures + text + equations-as-text), multilingual (papers in EN / AR / ZH / RU). 8K context = whole abstract in one embedding. Vamana index (literature corpus is essentially static). Pure search — no LLM dependency.

### 5.6 Investigative / Compliance

#### 5.6.1 `Witness` — investigative journalism / legal discovery tool
Index huge corpora of leaked docs (text), evidence images, interview audio. Hybrid search across all. Pure search, no LLM, fits the parked-RAG constraint. Privacy-first (no cloud). The in-DB RRF reranker fuses exact-phrase hits (FTS) with semantic "what is this really about" hits (dense), which is exactly the workflow investigative journalists need.

---

## 6. Round 2 — Ideas That Deliberately Do NOT Use ZVec.NET

Taste matters. These are deliberately excluded because ZVec.NET would add cost without value.

- **Real-time streaming classification / moderation** — use a fine-tuned classifier or a kNN over a flat array; a vector DB adds latency you don't need.
- **Cold generative chat with no retrieval** — if there's nothing to retrieve, the DB is dead weight; ship a thin embedder + LLM.
- **Cross-tenant shared search** — the one place hosted vector DBs still win (shared corpora, multi-tenant ACL). Partner instead of competing.
- **Anything that requires Blazor WebAssembly as the only delivery target** — ZVec.NET does not ship a WASM RID; pick a different stack or accept a server-side Blazor / desktop architecture.
- **Streaming / vectorized ETL pipelines** — you need columnar storage (Arrow / Parquet), not a vector DB; the vector DB comes in *after* the ETL, not during.
- **Learned-to-rank recommender systems** — ANN retrieval is only the *retrieval* stage; the ranking stage needs a learned model with features, and a vector DB doesn't help you there.
- **HNSW-RaBitQ on ARM devices** — the upstream ISA is x86_64 + AVX2 only; if your target is ARM mobile, use plain HNSW instead.
- **DiskANN on Windows / macOS** — upstream is Linux-only; if your target is non-Linux desktop, use HNSW or Vamana.

---

## 7. Parked Ideas (need an LLM side-car)

These are explicitly parked per scope. They would be valuable, but they require an LLM to generate answers, which adds an external dependency (cloud LLM API, on-device Gemma 3 decoder, or similar). They are kept on the list so we can revisit them when the LLM story matures.

- **`DropRAG`** — drag-and-drop confidential document Q&A. The retrieval half is preserved in `LegalLens` (pure search); the generation half is parked.
- **`Recall.Chat`** — a conversational interface over your `Recall` corpus. The retrieval layer is already built; the chat layer waits for a suitable on-device LLM.
- **`EchoNote.Transcribe`** — embedding voice memos is already in `EchoNote`; turning them into structured transcripts requires an ASR model (separate from the embedder).
- **`PolyglotPal.Translate`** — semantic TM is already in `PolyglotPal`; actual translation requires an MT model.

The pattern: every parked idea is a *generation* layer that sits on top of a *retrieval* layer the stack already supports. When the generation layer is ready, these become shippable with no changes to the retrieval layer.

---

## 8. Recommendation Matrix — What to Build First

Scoring: **Feasibility** (can we ship this in 30 days with the current beta SDK?), **Defensibility** (how hard is it for a hosted-DB competitor to copy?), **Wow-factor** (does the demo make jaws drop?), **Stack coverage** (does it exercise the differentiating capabilities?).

| Idea | Feasibility | Defensibility | Wow-factor | Stack coverage | Recommended |
|---|---|---|---|---|---|
| `Recall.MAUI` (POC) | High | Very High | Very High | Very High | **Yes — 30-day POC** |
| `PhotoMind` | Very High | High | High | High | **Yes — quick win** |
| `LegalLens` | High | High | Medium | High | Yes — vertical wedge |
| `CodeSense` | High | Medium | Medium | High | Yes — dev-tool wedge |
| `AOT-RAG Edge Function` | High | High | Medium | High | Yes — deployment pattern |
| `PolyFind` | Medium | High | Very High | Very High | Yes — second wave |
| `EchoNote` | High | High | High | High | Yes — second wave |
| `FieldGuide` | Medium | High | High | High | Yes — vertical wedge |
| `Vamana Archive` | Medium | Medium | Medium | Medium | Yes — long tail |
| `DiskANN Vault` | Medium | High | Medium | Medium | Yes — enterprise wedge |
| `MedSearch` | Medium | Very High | Medium | High | Yes — regulated vertical |
| `RetailRover` | Medium | Medium | Medium | Medium | Maybe — vertical wedge |
| `Unity AssetMind` | Medium | Medium | High | High | Maybe — ecosystem play |
| `Synapse` | High | Medium | Medium | Medium | Maybe — knowledge-worker wedge |
| `Witness` | Medium | Very High | Medium | Medium | Yes — impact vertical |

### 8.1 Suggested sequencing

1. **Day 0–30 — `PhotoMind` POC.** Smallest scope, instant wow, validates the MAUI Blazor Hybrid + ZVec.NET + ONNX Runtime path on Android and iOS. Multimodal + multilingual + offline all in one demo.
2. **Day 30–90 — `Recall.MAUI`.** Graduate from `PhotoMind` to the full multimodal "second brain". Add audio + video + screen-state capture. Add MRL tiering and task prefixes.
3. **Day 90–120 — `LegalLens` and `CodeSense`.** Vertical wedges that monetize the same core. Prove the FTS + dense + RRF hybrid search story in two paying verticals.
4. **Day 120+ — `Recall` desktop variant** (Native AOT single-binary for Windows/macOS/Linux) **with P2P sync**. This is the flagship.
5. **Long tail — `Vamana Archive`, `DiskANN Vault`, `MedSearch`, `Witness`.** Each is a regulated or scale-limited vertical where the privacy + scale + offline story is unbeatable.

---

## 9. What I'd Want to Verify Next

Honest gaps from this round of research, in case a future iteration wants to dig deeper:

- **Real-world latency numbers** for ZVec.NET on mid-tier Android (Pixel 6a class) at 100k / 1M / 10M vectors with HNSW. The README claims in-process performance but does not publish mobile benchmarks.
- **ONNX Runtime mobile footprint** for EmbeddingGemma 2 with selectively-loaded encoders. The 270M text-only mode is the critical path for mobile ingestion; we need measured startup time and RSS.
- **MRL truncation quality curve** at 128d for *multimodal* queries (the published benchmarks focus on text). If 128d multimodal holds quality, the storage math for `Recall` at 1M+ items becomes very comfortable.
- **DiskANN on `linux-arm64`** — supported in ZVec.NET? The README implies Linux-only but does not explicitly state the ARM64 status.
- **Vamana build time** for a 100M-vector static catalog — this gates whether `Vamana Archive` is a days-or-months proposition for the museum/archive vertical.
- **MAUI iOS RID stability** — `ios-arm64` is listed as Mobile HARD in beta.4, but I'd want a reference deployment before betting a flagship on it.

---

## 10. Sources

- `google/embeddinggemma-2` — Hugging Face model card: <https://huggingface.co/google/embeddinggemma-2>
- `onnx-community/embeddinggemma-2-ONNX` — official ONNX export: <https://huggingface.co/onnx-community/embeddinggemma-2-ONNX>
- Google AI for Developers — EmbeddingGemma docs: <https://ai.google.dev/gemma/docs/embeddinggemma>
- Google Developers Blog — EmbeddingGemma 2 Developer Guide: <https://developers.googleblog.com/embeddinggemma-2-the-developer-guide>
- Google Blog — EmbeddingGemma 2 launch post: <https://blog.google/innovation-and-ai/technology/developers-tools/embeddinggemma-2>
- `ahmedSamir50/AdamSystems.ZVec.NET` — GitHub: <https://github.com/ahmedSamir50/AdamSystems.ZVec.NET>
- ZVec.NET docs site: <https://ahmedsamir50.github.io/AdamSystems.ZVec.NET/>
- `alibaba/zvec` — upstream in-process vector DB: <https://github.com/alibaba/zvec>
- ZVec official site: <https://zvec.org>
- ZVec architecture blog: <https://zvec.org/en/blog/2026-04-29-zvec-architecture>
- Ollama `embeddinggemma-2` library entry: <https://ollama.com/library/embeddinggemma-2>
