---
name: recall-expert-harness
description: >-
  Convenes the Recall expert panel to review or harden app docs and to route
  implementation work to the right expert. Use when the user mentions experts,
  the panel, harness, hardening docs, Latest App Docs, InitDocs review, or
  asks which specialist should own a Recall decision.
disable-model-invocation: true
---

# Recall expert harness

Recall is a local-only .NET 10 MAUI Blazor Hybrid app: EmbeddingGemma 2 (ONNX) + ZVec.NET. `InitDocs/` is the initial idea, not the final spec.

## When to use this

- The user wants the experts to discuss or harden documentation.
- A change crosses more than one specialty (indexing and UX, capture and privacy, tests and embeddings).
- You are about to invent a stack choice, a CI host, an OCR engine, telemetry, or a sync implementation.

For a narrow change, read the one matching skill under `.cursor/skills/<id>/SKILL.md` and follow it. Do not load the whole panel.

## Required reads

1. [canon.md](canon.md) — current intent and known InitDocs drift
2. [panel.md](panel.md) — who owns what
3. [review-protocol.md](review-protocol.md) — only when the task is a doc review

## Rules

- Canon wins over a stale InitDocs sentence. Flag the sentence; do not implement it.
- Do not close an unresolved item in `canon.md` by picking a side. Ask, or leave it open.
- Doc reviews produce finding blocks and stop for acceptance before edits.
- Adding an expert means: a skill, an agent file with the same id, and a row in `panel.md`. Then run `tools/expert-harness/Test-ExpertPanel.ps1`.

## Expert ids

`dotnet-architect`, `vectors-embeddings`, `nlp`, `maui-platform`, `ux-ui`, `privacy-local-first`, `performance`, `unit-tests`, `retrieval-qa`, `docs-mkdocs`
