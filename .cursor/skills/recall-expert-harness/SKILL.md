---
name: recall-expert-harness
description: >-
  Signs Recall plans in one shared context for @recall: or @reacall:.
  Do not spawn a subagent per expert. Use when the user writes @recall:,
  @reacall:, asks for a project, milestone, or feature plan, or mentions
  the harness, experts, or hardening docs.
disable-model-invocation: true
---

# Recall expert harness

Recall is a local-only .NET 10 MAUI Blazor Hybrid app: EmbeddingGemma 2 (ONNX) + ZVec.NET. `InitDocs/` is the initial idea, not the final spec.

## Planning trigger

A message that contains `@recall:` or `@reacall:`, or that asks for a project, milestone, phase, or feature plan, runs the full panel in this chat. Follow [planning-chain.md](planning-chain.md). Do not launch expert subagents. Do not draft the plan yourself and then ask the experts to nod.

Each expert returns the block in [sign-off.md](sign-off.md). The parent publishes that YAML in the reply and in the plan on every gate, including a later message that changes the plan. Rubber-stamping fails the chain. The user does not ask for the sign-off.

## Other work

- Doc hardening uses [review-protocol.md](review-protocol.md). That is not a substitute for a `@recall:` plan.
- A narrow code change reads one skill under `.cursor/skills/<id>/SKILL.md`. It still obeys [development-rules.md](development-rules.md) and [test-honesty.md](test-honesty.md).

## Required reads

1. [canon.md](canon.md)
2. [panel.md](panel.md)
3. [development-rules.md](development-rules.md) before any plan or product code
4. [planning-chain.md](planning-chain.md) when the trigger fires

## Rules

- Canon wins over a stale InitDocs sentence. Flag the sentence.
- Development rules win over a plan that breaks them. The expert pushes back with a fix, not a slogan.
- Do not close an unresolved canon row by picking a side.
- Adding an expert means a skill, an agent file with the same id, a panel row, and a line in the planning chain. Then run `tools/expert-harness/Test-ExpertPanel.ps1`.

## Expert ids

`dotnet-architect`, `vectors-embeddings`, `nlp`, `maui-platform`, `ux-ui`, `privacy-local-first`, `performance`, `unit-tests`, `retrieval-qa`, `docs-mkdocs`
