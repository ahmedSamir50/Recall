# Planning chain

A project plan is not written by the parent alone. The full panel signs it.

## Trigger

Run this chain when the user message contains `@recall:` or `@reacall:`, or when they ask for a project, milestone, phase, or feature plan.

The text after the colon is the plan. If they only wrote the token, ask what the plan is and do not invent one. Do not start implementation in the same turn.

A one-file bugfix does not use this chain. It still follows `development-rules.md` and `test-honesty.md`.

## Steps

1. Read [canon.md](canon.md), [development-rules.md](development-rules.md), [test-honesty.md](test-honesty.md), [sign-off.md](sign-off.md), and [panel.md](panel.md).
2. Launch all ten experts in one parallel batch. `subagent_type` is the expert id. Environment is local. Do not pick a model unless the user named one.
3. Each prompt contains the plan verbatim plus the instructions below. Subagents do not see the user message unless you paste it.
4. If a sign-off is missing a field, has an empty checked list, or agrees or rejects with no evidence, resume that expert once and ask for the missing evidence. Do not rewrite their verdict yourself.
5. Publish every sign-off in full. Then write the synthesis below.
6. Stop. The user decides which recommendations enter the plan. Do not code from an unsigned plan.

## Expert ids

`dotnet-architect`, `vectors-embeddings`, `nlp`, `maui-platform`, `ux-ui`, `privacy-local-first`, `performance`, `unit-tests`, `retrieval-qa`, `docs-mkdocs`

## Prompt to give each expert

```
You are the Recall expert <id> on a planning sign-off.
Read .cursor/skills/<id>/SKILL.md,
.cursor/skills/recall-expert-harness/canon.md,
.cursor/skills/recall-expert-harness/development-rules.md,
.cursor/skills/recall-expert-harness/test-honesty.md,
and .cursor/skills/recall-expert-harness/sign-off.md.

Plan, verbatim:
<paste>

Research the repo and InitDocs before you answer. Push back when the plan, the user, or the docs are inconsistent, incomplete, or logically wrong. Agree only if the checked list supports it. Return only the sign-off block. Do not edit files.
```

## Synthesis

After the ten blocks:

```markdown
## Plan synthesis
Trigger:
Blockers:
Push-backs:
Conditions:
Conflicts between experts:
Test matrix gaps:
Recommended plan changes:
Left unresolved on purpose:
```

Lead with blockers and push-backs. Do not average a `block` into an approval. `unit-tests` and `retrieval-qa` can veto a plan that has no honest test matrix. `docs-mkdocs` checks that the resulting plan would not cite a stale InitDocs sentence as current.

## After the user accepts

Update `canon.md` only for decisions they accepted. Then implementation follows the owning skills, the development rules, and the test matrix. A later `@recall:` plan runs the full chain again.
