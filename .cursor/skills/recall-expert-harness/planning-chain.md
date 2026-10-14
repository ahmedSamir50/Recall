# Planning chain

A project plan is not written by the parent alone. The full panel signs it in this chat, after one read of the shared files.

Do not launch expert subagents. Each subagent has its own context, so ten of them re-read the same canon, rules, InitDocs, and worktree and multiply the token cost by the panel size. That fan-out is forbidden for a plan, a code review, a follow-up, and a doc pass.

## Trigger

Run this chain when the user message contains `@recall:` or `@reacall:`, or when they ask for a project, milestone, phase, or feature plan.

The text after the colon is the plan. If they only wrote the token, ask what the plan is and do not invent one. Do not start implementation in the same turn.

A one-file bugfix does not use this chain. It still follows `development-rules.md` and `test-honesty.md`.

## Steps

1. Read [canon.md](canon.md), [development-rules.md](development-rules.md), [test-honesty.md](test-honesty.md), [sign-off.md](sign-off.md), and [panel.md](panel.md) once. If this conversation already holds a file, do not read it again.
2. Read the plan's named paths once. For a review of current changes, read the diff once. Do not open InitDocs unless the plan cites a passage.
3. Stay in this context. Apply each expert checklist below and write that expert's YAML sign-off here. Open `.cursor/skills/<id>/SKILL.md` only when the plan touches that expert's row in [panel.md](panel.md). An untouched row still gets a sign-off, with `checked` citing `panel.md` and the scope miss, and that skill file stays unread.
4. If a sign-off is missing a field, has an empty checked list, or agrees or rejects with no evidence, rewrite that block from the files already loaded. Do not spawn the expert to go look again.
5. Publish the YAML list in the user-visible reply, first. Do not rewrite a short field into a paragraph. Then write the synthesis below. A plan card, a count, or a summary is not a sign-off.
6. Write that same YAML into the open plan under `## Expert sign-offs` before the turn ends. Create the plan only after the blocks exist. If a plan tool creates the file first, edit the file in that same turn.
7. Stop. The user decides which recommendations enter the plan. Do not code from an unsigned plan.

## Same plan, later message

A later message that closes a decision, changes scope, or corrects what a source means is the same gate. Re-sign in this context from files already loaded. Re-read only a path the new message names that is not already loaded. Replace the YAML in the reply and in the plan. Do not keep the previous verdicts. Do not launch expert subagents. Do not answer, and do not edit the plan's behavior, before the new blocks are in both places.

The user does not ask for this. Do not ask the user why a sign-off is missing or unchanged. A question about the harness display is not a new plan. A request to execute the plan is not a new plan.

## Expert ids

`dotnet-architect`, `vectors-embeddings`, `nlp`, `maui-platform`, `ux-ui`, `privacy-local-first`, `performance`, `unit-tests`, `retrieval-qa`, `docs-mkdocs`

## How each sign-off is made

The parent writes every block. Push back when the plan is inconsistent, incomplete, or logically wrong. Push back when the plan does not name the files, folders, and literals a later agent would otherwise invent. Agree only if the checked list supports it. Two sentences maximum in each field. Do not edit product files in this turn.

## How detailed the plan must be

The Phase 1 and Phase 2 plan was accepted as a short task list. The following code review then changed more than 58 files the plan had not named. A less capable model invented folder layout, literals, async shapes, and user-facing sentences. That fails this chain.

Before the plan is something a later agent may execute, the synthesis must include a file manifest:

- Every file to add, edit, or move, with its folder. Group by use (device identity, download, text encoder), not a flat pile of types.
- Every new literal classified as a canon constant, an `appsettings` key, or both resx files. The resx cells are the actual Arabic and English sentences, written for a person, not for a log.
- The public method and the branch a test will lock. "Add search" is not a task.
- What the agent must not invent: extra projects, extra files, a second copy of a rule, a placeholder benchmark, Arabic-Indic digits, or a file extension the plan did not name.

An expert pushes back when the pasted plan would leave those choices open. `unit-tests` pushes back when the only benchmark returns a constant.

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

## After a `@recall:` plan is executed

The turn that carries out an accepted `@recall:` plan updates the single progress table at the end of `docs/plan.md` before it says the work is done. That table is the only progress board. Do not add a second table, and do not put percentages in the phase or epic headings.

- A bold phase row is the header: status and percentage.
- A bold epic row under that phase is the subheader: status and percentage.
- Each task row under that epic: status and percentage.
- The last row is the footer for all epics: status and percentage.
- A task is 100 when `done`, 50 when `partial`, and 0 when `not-started`.
- An epic percentage is the mean of its tasks, rounded to the nearest whole number. A phase percentage is the mean of its tasks. The footer is the mean of every task in Phases 1–7.
- Parent words stay on the all-or-nothing rule: `done` only when every child is `done`, `not-started` only when every child is `not-started`, otherwise `partial`.
