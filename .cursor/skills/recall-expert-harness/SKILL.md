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

Each expert returns the block in [sign-off.md](sign-off.md). The parent writes that YAML once, in the plan. A later gate is a new user message, and that turn does not re-sign itself. The chat reply is the synthesis and does not paste the YAML again. Rubber-stamping fails the chain. Close a gap the loaded files can close, then score again once. That second score is final. A third pass is a loop. The user does not ask for the sign-off. If a direct expert's final score is under 96, say the plan is not ready and stop.

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
- An accepted plan is an execution manifest. The implementing agent and the code review do not invent files, signatures, branches, or sentences.
- A request to close a task, epic, or phase finishes that parent in the same plan unless a sign-off names a blocker. Another plan for the same close is a failed execution.
- Sign-off YAML is written once, in the plan. The first line is `Direct:` and the main experts. An id not on that line is advisory. Confidence is honest. Every expert does a best accountable effort. A direct expert under 96 names a mitigation and a recommendation, and the plan is not ready for approval.
- Do not close an unresolved canon row by picking a side.
- Adding an expert means a skill, an agent file with the same id, a panel row, and a line in the planning chain. Then run `tools/expert-harness/Test-ExpertPanel.ps1`.

## Expert ids

`dotnet-architect`, `vectors-embeddings`, `nlp`, `maui-platform`, `ux-ui`, `privacy-local-first`, `performance`, `unit-tests`, `retrieval-qa`, `docs-mkdocs`
