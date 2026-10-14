# Sign-off

A `@recall:` / `@reacall:` expert returns this YAML and nothing else. Do not edit the repo.

```yaml
- expert: <id>
  verdict: approve | approve-with-conditions | push-back | block
  confidence: <0-100>
  checked: "<one line: claim — path>"
  pushback: "<one line, or none>"
  mitigation: "<the gap when a direct expert is under 96, or when an advisory finding needs an action, otherwise none>"
  concerns:
    - "<one line>"
  recommendation: "<the action that closes the gap, or none when nothing is left>"
```

Rules:

- `checked` names a file. "The doc says so" is not a check.
- `pushback` is `none` only when `checked` supports the verdict.
- `concerns` has at least one line. A rubber stamp with an empty list is a failed sign-off.
- `recommendation` is one path. Options and essays stay out of this block.
- `notes` is not a field. Do not add it.
- Confidence is the honest result of the check. There is no floor of 80. Rounding a score up fails the sign-off. A low score used to skip the check fails it too.
- The first line under `## Expert sign-offs` is `Direct:` and one or more ids from `panel.md`. An id not on that line is advisory. Do not write an advisory list. Do not search for an expert who is not in that table.
- Every expert, direct and advisory, checks their own row once. When that check finds a gap the loaded files can close, close it, then score again once. That second score is final. A third pass is a loop.
- The user will not approve a plan while any direct expert's final score is under 96. That verdict is `push-back` or `approve-with-conditions`. `mitigation` names the gap. `recommendation` names the action. Say the plan is not ready and stop.
- An advisory expert is not held to 96. An advisory `push-back` or `block` still keeps the plan not ready.
- A direct `approve` needs an honest confidence of 96 or higher, `mitigation: none`, no material gap, and an execution manifest. A plan that leaves a file, signature, branch, sentence, or test assertion for the implementing agent to choose is `push-back`.
- `approve-with-conditions` means you approve if `recommendation` is adopted.
- `push-back` means wrong logic, a missing branch test, or a rule break.
- `block` means a canon non-negotiable or a test-honesty failure this plan cannot meet.

The parent writes this once in the plan. The chat does not paste it again, and a field is not expanded into a paragraph.

```markdown
## Expert sign-offs

Direct: <one or more ids>
```

The YAML blocks follow that line. An id not on it is advisory.
