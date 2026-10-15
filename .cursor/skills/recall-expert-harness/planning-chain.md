# Planning chain

A project plan is not written by the parent alone. The full panel signs it in this chat, after one read of the shared files.

Do not launch expert subagents. Each subagent has its own context, so one per expert re-reads the same canon, rules, InitDocs, and worktree and multiplies the token cost by the panel size. That fan-out is forbidden for a plan, a code review, a follow-up, and a doc pass.

## Trigger

Run this chain when the user message contains `@recall:` or `@reacall:`, or when they ask for a project, milestone, phase, or feature plan.

The text after the colon is the plan. If they only wrote the token, ask what the plan is and do not invent one. Do not start implementation in the same turn.

A one-file bugfix does not use this chain. It still follows `development-rules.md` and `test-honesty.md`.

## Steps

1. Read [canon.md](canon.md), [development-rules.md](development-rules.md), [test-honesty.md](test-honesty.md), [sign-off.md](sign-off.md), and [panel.md](panel.md) once. If this conversation already holds a file, do not read it again.
2. Read the plan's named paths once. For a review of current changes, read the diff once. Do not open InitDocs unless the plan cites a passage.
3. Stay in this context. Apply each expert checklist once and keep that sign-off for the plan file. Open `.cursor/skills/<id>/SKILL.md` only when the plan touches that expert's row in [panel.md](panel.md). An untouched row still gets one sign-off, with `checked` citing `panel.md` and the scope miss, and that skill file stays unread.
4. Name one or more direct experts from the ids in [panel.md](panel.md). Every other listed id is advisory. Do not search for an expert who is not in that table. When a check finds a gap the loaded files can close, close it, then score again once. That second score is final. A third pass is a loop. Do not round a score up to 96.
5. Write the sign-off once in the plan. The first line under `## Expert sign-offs` is `Direct:` and the direct ids. The YAML follows. An id not on that line is advisory. Do not write an advisory list. Do not paste that YAML in the chat. The chat reply is the synthesis below. A plan card, a count, or a summary is not a sign-off.
6. Create the plan only after the blocks exist. If a plan tool creates the file first, edit the file in that same turn. The chat does not repeat the blocks.
7. Stop. Do not reopen this chain in the same turn. The user decides which recommendations enter the plan. Do not code from an unsigned plan.

## Same plan, later message

A new user message that closes a decision, changes scope, or corrects what a source means is the same gate. The turn that is writing the plan does not re-sign itself. Re-sign once from files already loaded. Re-read only a path the new message names that is not already loaded. Replace the YAML in the plan. Do not paste it in the reply. Do not keep the previous verdicts. Do not launch expert subagents. Do not answer, and do not edit the plan's behavior, before the new blocks are in the plan.

The user does not ask for this. Do not ask the user why a sign-off is missing or unchanged. A question about the harness display is not a new plan. A request to execute the plan is not a new plan.

## Expert ids

`dotnet-architect`, `vectors-embeddings`, `nlp`, `maui-platform`, `ux-ui`, `privacy-local-first`, `performance`, `unit-tests`, `retrieval-qa`, `docs-mkdocs`

## How each sign-off is made

The parent writes every block. Push back when the plan is inconsistent, incomplete, or logically wrong. Push back when the plan does not name the files, folders, and literals a later agent would otherwise invent. Agree only if the checked list supports it. Two sentences maximum in each field. Do not edit product files in this turn.

## How detailed the plan must be

The implementing agent is less capable than the planner. It invents every choice the plan leaves open, and the code review invents a second time. One gate implemented 47 files and the review rewrote more than 40. Another implemented 28 and the review rewrote more than 20. The Phase 1 and Phase 2 plan was a short task list, and its review changed more than 58 files the plan had not named. A short task list fails this chain.

A plan is not executable until the synthesis contains an execution manifest whose four lists are filled with concrete lines. A heading with nothing under it does not count. `approve` without that manifest is a failed sign-off. `dotnet-architect` blocks it.

- **Files.** One line per file: full path, add or edit or move, the type and method signature, and the branch (input, result, and the failure result). Group by use.
- **Literals.** One line per new literal: a canon constant, an `appsettings` key, or both resx files. The resx cells are the Arabic sentence and the English sentence a person will read.
- **Tests.** One line per test: method name, the production method and branch it locks, and the assertion value. "Add tests" is not a line. A benchmark that returns a constant is not a line.
- **Do not invent.** Extra projects, extra files, extra types, a second copy of a rule, a neighbor refactor, a placeholder benchmark, Arabic-Indic digits, and any extension the plan did not name. If a choice is not a line above, it belongs on this list.

The implementing agent and the code review follow "Executing an accepted plan" in the always-applied rule. They do not fill gaps.

## Closing a task, epic, or phase

When the user says to close a task, epic, or phase, that plan finishes it. Epic 1 and Epic 2 were requested closed, more than two plans were written, and no phase or epic reached `done`. That fails this chain.

The manifest includes every remaining `partial` and `not-started` child of that parent, each with a file, a branch, and a test, until the parent's done-when check is true. A leftover child is allowed only when a sign-off names a blocker, a dependency outside the repo, or a push-back the user has not accepted. "Left for a later plan" is not a blocker. `dotnet-architect` blocks a close request that stops at `partial` with no named blocker.

## Synthesis

After every expert on the panel has a block:

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
Ready for approval: yes only when every id on the Direct line is honestly at 96 or higher and no advisory block is a push-back or a block

## Execution manifest
Files:
Literals:
Tests:
Do not invent:
```

Lead with blockers and push-backs. Do not average a `block` into an approval. `unit-tests` and `retrieval-qa` can veto a plan that has no honest test matrix. `docs-mkdocs` checks that the resulting plan would not cite a stale InitDocs sentence as current. An empty execution manifest means the plan is not executable.

## After the user accepts

Update `canon.md` only for decisions they accepted. Implementation and the later code review obey the execution manifest. They do not design. A later `@recall:` plan runs the full chain again. A second plan does not finish a close the previous plan left `partial` unless the sign-off named a blocker the user has now accepted.

## Docs

Every plan execution manifest has a docs list. Each line is a docs/ path, add or edit, and the sentences that change. Execution updates those pages before it updates the progress table. The user does not ask for this. A behavior change with an empty docs list is a failed sign-off from docs-mkdocs, unless the sign-off says there is no docs surface.

## After a `@recall:` plan is executed

The turn that carries out an accepted `@recall:` plan updates the single progress table at the end of `docs/plan.md` before it says the work is done. That table is the only progress board. Do not add a second table, and do not put percentages in the phase or epic headings.

- A bold phase row is the header: status and percentage.
- A bold epic row under that phase is the subheader: status and percentage.
- Each task row under that epic: status and percentage.
- The last row is the footer for all epics: status and percentage.
- A task is 100 when `done`, 50 when `partial`, and 0 when `not-started`.
- An epic percentage is the mean of its tasks, rounded to the nearest whole number. A phase percentage is the mean of its tasks. The footer is the mean of every task in Phases 1–7.
- Parent words stay on the all-or-nothing rule: `done` only when every child is `done`, `not-started` only when every child is `not-started`, otherwise `partial`.
- A close request ends with that parent `done` and 100, or the turn names the blocker from the sign-off and stops. Leaving the parent `partial` and starting another plan for the same close fails the execution.
