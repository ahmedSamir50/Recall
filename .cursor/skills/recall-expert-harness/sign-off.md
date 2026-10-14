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
- Each plan names one or more direct experts from the whole panel. Every other expert on that plan is advisory. The panel is every id in `panel.md`, including ids added later.
- Every expert, direct and advisory, checks their own row against canon, the named files, and the decisions those files already make before scoring. A gap that check can close is unfinished work: close it, then score again honestly.
- The user will not approve a plan while any direct expert is under 96. That expert's verdict is `push-back` or `approve-with-conditions`. `mitigation` names the gap. `recommendation` names the action. Do not present the plan as ready.
- An advisory expert is not held to 96. An advisory `push-back` or `block` still keeps the plan not ready.
- A direct `approve` needs an honest confidence of 96 or higher, `mitigation: none`, no material gap, and an execution manifest. A plan that leaves a file, signature, branch, sentence, or test assertion for the implementing agent to choose is `push-back`.
- `approve-with-conditions` means you approve if `recommendation` is adopted.
- `push-back` means wrong logic, a missing branch test, or a rule break.
- `block` means a canon non-negotiable or a test-honesty failure this plan cannot meet.

The parent writes the YAML once, under `## Expert sign-offs` in the plan. It does not paste the same list in the chat, and it does not expand a field into a paragraph.
