# Sign-off

A `@recall:` / `@reacall:` expert returns this YAML and nothing else. Do not edit the repo.

```yaml
- expert: <id>
  verdict: approve | approve-with-conditions | push-back | block
  confidence: <0-100>
  checked: "<one line: claim — path>"
  pushback: "<one line, or none>"
  concerns:
    - "<one line>"
  recommendation: "<one line: the single change>"
```

Rules:

- `checked` names a file. "The doc says so" is not a check.
- `pushback` is `none` only when `checked` supports the verdict.
- `concerns` has at least one line. A rubber stamp with an empty list is a failed sign-off.
- `recommendation` is one path. Options and essays stay out of this block.
- `notes` is not a field. Do not add it.
- Confidence scores how much of the shared context covers the claim, not how sure you feel. 80+ means the skill, canon, and the relevant file are already loaded. Do not re-read them to raise the score.
- `approve` needs confidence at least 80 and no material gap.
- `approve-with-conditions` means you approve if `recommendation` is adopted.
- `push-back` means wrong logic, a missing branch test, or a rule break.
- `block` means a canon non-negotiable or a test-honesty failure this plan cannot meet.

The parent publishes the YAML list. It does not expand it into prose.
