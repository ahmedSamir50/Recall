# Sign-off

A `@recall:` / `@reacall:` expert returns this YAML and nothing else. Do not edit the repo.

```yaml
- expert: <id>
  verdict: approve | approve-with-conditions | push-back | block | out-of-scope
  confidence: <0-100>
  checked: "<one line: claim — path>"
  pushback: "<one line, or none>"
  mitigation: "<the gap the check found, or none when nothing is left>"
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
- Confidence is the honest result of the check. There is no floor of 80. There is no numeric approval gate. Rounding a score up fails the sign-off. A low score used to skip the check fails it too. Do not raise a score so the plan looks ready. The Direct line does not change the number. Two different checks get two different numbers.
- The first line under `## Expert sign-offs` is `Direct:` and one or more ids from `panel.md`. An id not on that line is advisory. Do not write an advisory list. Do not search for an expert who is not in that table.
- Every expert, direct and advisory, checks their own row once on the same scale. When that check finds a gap the loaded files can close, close it, then score the closed check once. That second score is final and stays honest. A third pass is a loop.
- The user is the approval gate. A low score does not reject the plan and does not stop the turn. `mitigation` names the gap. `recommendation` names the action that closes it. The synthesis names an alternative. Hand the plan to the user with those three.
- A `push-back` or `block`, direct or advisory, is reported the same way. It does not end the turn. The user accepts, rejects, or sends the plan back.
- A rejection keeps the honest scores. The next turn closes the gaps the user named, writes the alternative the check supports, and scores that check. It does not raise a score because the last plan was rejected.
- An untouched row is `out-of-scope`. `checked` cites `panel.md` and the scope miss. Its confidence is only how sure that row is untouched. That block does not approve the plan.
- A direct `approve` needs `mitigation: none`, no material gap, and an execution manifest. Confidence is whatever that check supports. A plan that leaves a file, signature, branch, sentence, or test assertion for the implementing agent to choose is `push-back`.
- `approve-with-conditions` means you approve if `recommendation` is adopted.
- `push-back` means wrong logic, a missing branch test, or a rule break.
- `block` means a canon non-negotiable or a test-honesty failure this plan cannot meet.

The parent writes this once in the plan. The chat does not paste it again, and a field is not expanded into a paragraph.

```markdown
## Expert sign-offs

Direct: <one or more ids>
```

The YAML blocks follow that line. An id not on it is advisory.
