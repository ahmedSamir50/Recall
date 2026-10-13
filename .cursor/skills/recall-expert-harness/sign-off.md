# Sign-off

Every expert on a `@recall:` / `@reacall:` plan returns this block and nothing else. Do not edit the repo during the sign-off.

```markdown
### <expert-id>
Verdict: approve | approve-with-conditions | push-back | block
Confidence: <0-100>
Confidence basis: <what you read and what you did not check>
Checked:
- <claim> — <evidence: file, section, or code path>
Notes:
- <constraint, gap, or confirmation that matters to this plan>
Recommendations:
- <the change you want, or "none">
Options:
- <a way to fix this inside the current design, or "none — no defect in scope">
Alternatives:
- <a different design, or "none — current design holds">
Risks:
- <what goes wrong if the plan ships unchanged> — mitigation: <what reduces it>
Push-back:
- <the gap, bad logic, or inconsistency, including when the user or InitDocs caused it; or "none — the checked list holds">
```

## How to choose a verdict

Research first. Read your skill, `canon.md`, `development-rules.md`, `test-honesty.md`, and the parts of `InitDocs/` and the repo the plan touches. The user's wording and a doc sentence are inputs, not proof.

| Verdict | When |
|---|---|
| `approve` | No material gap in your scope. Confidence is at least 80. The checked list names the evidence. |
| `approve-with-conditions` | You would approve if the recommendations land in the plan. Name the condition as testable. |
| `push-back` | Wrong logic, a contradiction, a missing branch test, a magic string, an untranslated string, a god type, a leak, or a cost the budget does not allow. Confidence is usually 40–79. |
| `block` | A non-negotiable in `canon.md`, or test honesty that this plan cannot meet. Confidence is usually under 40 when the evidence is thin, and may be high when the violation is obvious. |

Agreeing because the doc says so, or rejecting because you dislike the direction, is a failed sign-off. The parent sends that expert back once with the missing evidence named.

## Empty-looking sections

`Options` and `Alternatives` stay filled when you approve: write `none — <what you checked>`. `Push-back` is `none` only with that same checked list. A block with no mitigation is incomplete; state the mitigation even if it is "cut this from the plan".

## Confidence

Score how well you checked, not how strongly you feel.

- **80–100** — skill, canon, and the relevant doc or code were read; the claim is either supported or refuted.
- **50–79** — the defect or the fit is likely, and a measurement or a missing file still sits outside this review.
- **0–49** — you are blocking or pushing back on incomplete evidence, and you say what would raise the score.
