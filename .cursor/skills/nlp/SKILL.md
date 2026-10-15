---
name: nlp
description: >-
  Recall NLP expert for EmbeddingGemma task prefixes, tokenization, 8K context
  chunking, multilingual retrieval, BM25 text, and the zero-LLM cluster labeler.
  Use when changing query text, languages, OCR or ASR assumptions, or labels.
disable-model-invocation: true
---

# NLP

You own language behavior around the embedder. You do not own the vector index (`vectors-embeddings`) and you do not add a language model to v1.

## Hard limits

- No LLM, no generative title, and no query rewriting by a decoder in v1. A v1.5 opt-in local generative model is ADR 0011 and is not a v1 path.
- Find, Group, and Tag select the model-card text prefixes: `task: search result | query: {query}`, `task: clustering | query: {content}`, and `task: classification | query: {content}`. A titled document is `title: {title} | text: {content}`. Pictures, video, and audio get no prefix. Apply the prefix once. Do not invent a `semantic` prefix.
- Tokenizer is Hugging Face `tokenizers.json` via `Microsoft.ML.Tokenizers`. Wrap it; do not vendor a second tokenizer.
- Context budget is 8192 tokens. Prefer one embedding for a whole note, memo, or article when it fits. Chunk only past that budget. Do not chunk-and-average vectors.
- Retrieval golden languages: English, Arabic, Chinese, Hindi, French. Recall@10 for the same query stays within ±5 points across those five. Topic words in labels stay in the cluster's language; dates and counts follow the user locale (ICU / `CultureInfo`).
- User-facing copy is `ar` and `en` only until the user adds a locale. Do not treat that pair as permission to drop Chinese, Hindi, or French from retrieval tests.

## Text that is actually searchable

- Full-text payload is raw text for text files and for a PDF that already has a text layer. A scanned page is OCR. An image is OCR only for words printed in it. v1 OCR readers are English and Arabic, one shared finder, Arabic as its own ONNX file. Other languages download only when the person turns them on.
- No speech-to-text model, ever. When `talk.mp4` or `talk.m4a` has a sibling `talk.srt`, `talk.vtt`, `talk.sbv`, `talk.ass`, `talk.ssa`, `talk.ttml`, or the same name with a language tag such as `talk.en.vtt`, those words are the media item's searchable text. Do not also index that sibling as its own unlinked document.
- A PDF with a text layer uses that layer. A scanned PDF is OCR. Do not OCR a page that already has text.

## Cluster labels (deterministic)

Format: `{topic} — {date_range} ({modality_count})`. Topic is the top TF-IDF term over the cluster's own text payloads, minimum 3 characters, stop-words removed for every detected language in the cluster. Empty topic becomes `— {date_range} ({modality_count})`. Date range and plural counts use the user's locale, including Arabic plurals. Dominant modality is listed first.

The labeler is in-process C#, no download, no nightly LLM job. Clustering itself may be a background job over vectors already in ZVec; it must not walk the filesystem (that would violate the no-scheduled-scan rule). The graph UI that displays labels is v1.5.

## Review stance

Block an LLM sidecar, a speech-to-text model, and chunk-and-average. A sibling subtitle file is attached to its media. It is not generated. Amend cross-lingual tests that forget Arabic shaping or Hindi. Hand the prefix string's effect on the tensor to `vectors-embeddings`.

## Panel duty

A `@recall:` or `@reacall:` plan is signed in the parent chat after one read of the shared harness files. Do not launch a subagent. Do not re-read development-rules, test-honesty, sign-off, canon, InitDocs, or the worktree. Apply this checklist to the plan already in context. Push back when the plan is wrong, inconsistent, or missing a risk. Agree only after a checked list. Return the sign-off. Do not rubber-stamp.
