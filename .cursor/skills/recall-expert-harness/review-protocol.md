# Review protocol

Use this when the user asks to harden, review, or discuss app docs with the experts. Do not start that review just because this harness exists.

## Roles of the documents

| Set | Role |
|---|---|
| `InitDocs/` | Initial idea. Input to the first hardening pass. Keep the files. Do not rewrite them in place unless the user says to. |
| Latest App Docs | The specification, once the user asks for it. Lives under `docs/` as MkDocs. Does not exist yet. |
| `canon.md` | Bridge. Experts obey it until Latest App Docs replace it. Update it only after the user accepts a canon change. |

## Steps

1. Read `canon.md` and `panel.md`.
2. Name the document under review and its status (InitDocs seed vs Latest App Docs).
3. Assign sections using the table in `panel.md`. Skip experts with nothing to review.
4. For each assigned expert, read `.cursor/skills/<id>/SKILL.md` and write one finding block. Do not edit the document in this step.
5. `dotnet-architect` lists conflicts between experts. Do not average them away.
6. `docs-mkdocs` checks that accepted changes would still form a coherent MkDocs book: one term per concept, no orphan claims, InitDocs labeled historical.
7. Show the synthesis to the user. Apply doc edits only after they accept. Unresolved rows in `canon.md` stay open until they decide.

## Finding block

```markdown
### <expert-id>
Scope:
Verdict: align | amend | block
Findings:
- [block|amend|note] <what is wrong or missing> — <why it matters> — <proposed change>
Unresolved:
- <canon U-ids or new questions, or "none">
```

Severity:

- **block** — breaks a non-negotiable in `canon.md`
- **amend** — stale, contradictory, or incomplete relative to canon
- **note** — clearer wording, missing measurement, or a future ADR

## Synthesis

Lead with the verdicts that block. Then amendments, grouped by expert. Then the unresolved table, with a recommended default only when the user asked for a recommendation. End with the exact files that would change if they accept.

During a review, experts propose. They do not create the MAUI solution, download models, or scaffold CI unless the user asked for that work in the same turn.
