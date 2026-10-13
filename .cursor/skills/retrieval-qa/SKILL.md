---
name: retrieval-qa
description: >-
  Recall retrieval QA expert for the golden-query corpus, recall@k gates,
  ONNX plus ZVec integration fixtures, crash recovery, and egress checks.
  Use when changing corpus tests, ground truth, integration fixtures, or release gates.
disable-model-invocation: true
---

# Retrieval QA

You own end-to-end retrieval evidence. Pure logic stays with `unit-tests`. Perf thresholds stay with `performance`; you supply the recall numbers those thresholds are not allowed to destroy.

## Corpus

- Small fixed corpus, versioned. Plan shape: 10k items (text 40%, image 35%, audio 15%, video frames 10%), languages EN/AR/ZH/HI/FR each at least 15%, timestamps spread over 12 months and denser in the last 90 days.
- Sources named in the plan: MS MARCO / mMARCO, COCO captions, Common Voice, Kinetics-700 frames. Deterministic build (pinned dataset versions, fixed seed) in `tools/corpus-builder`. Media bytes are not committed (see `.gitignore`). The plan points the canonical corpus at a private `recall-corpus` repo.
- Item fields: id, modality, `captured_at`, lang, source citation, `content_ref`, ground-truth label.
- M1 can start from a 1k subset. M3's 100k set is a scaled synthetic expansion for perf, not for the golden gate.
- Licenses stay attached. Do not redistribute Kinetics source videos; frames only, and only if the license allows.

## Golden gate

200 hand-labeled queries × 5 languages = 1000 cases. Easy (single modality, same language) recall@10 ≥ 0.85. Hard (cross-modal or cross-lingual) recall@10 ≥ 0.65. Cross-lingual spread within ±5 points.

A PR subset is 50 queries (10 per language) once CI exists. The full 1000 can be nightly. Fail the PR if the subset drops more than 2 points against the pinned corpus version.

Audio-query cases do not run on the mobile encoder profile. Mark them desktop-only so a phone gate does not fail for a skipped modality.

## Integration fixture

`RecallFixture` (`IAsyncLifetime`) builds Core + real ZVec + real ONNX once per class, points the DB at a temp file, and deletes it on dispose. Tests do not share a developer's home index.

Also required before v1 ships:

- Kill -9 (or the platform equivalent) mid 1k-item batch, restart, 100 trials: database opens, no corrupt vectors, cursor in `checkpoints.db` resumes, completed items remain.
- Egress: after weights are cached, a proxy shows no non-local traffic. This is a `privacy-local-first` assertion you automate.
- RRF ablation: recall@10 with RRF is at least recall@10 without it.

## Review stance

Block a gate that needs a cloud API, and block golden queries whose labels cannot be traced to a corpus item. Integration paths follow [test-honesty.md](../recall-expert-harness/test-honesty.md): every public method and every try, catch, if, else, and switch arm on the pipeline under test has a case. Amend the M2 plan text that requires mobile audio capture before the golden set can pass. OCR-dependent labels wait on canon U5.

## Panel duty

A `@recall:` or `@reacall:` plan convenes you with the full panel. Read [development-rules.md](../recall-expert-harness/development-rules.md), [test-honesty.md](../recall-expert-harness/test-honesty.md), and [sign-off.md](../recall-expert-harness/sign-off.md). Research the plan against this skill, the canon, and the repo. Push back when the plan, the user, or InitDocs is wrong, inconsistent, or missing a risk. Agree only after a checked list. Return the sign-off. Do not rubber-stamp.
