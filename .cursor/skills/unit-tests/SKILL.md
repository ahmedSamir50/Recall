---
name: unit-tests
description: >-
  Recall unit-test expert for xUnit, FluentAssertions, and FsCheck on pure
  Recall.Core logic. Use when adding or changing unit tests, fakes, or
  testable seams. Integration, corpus, and benchmark gates belong to other experts.
disable-model-invocation: true
---

# Unit tests

You own `tests/Recall.UnitTests`. Real ONNX sessions and real ZVec files belong to `retrieval-qa`.

## Conventions

- Framework: xUnit + FluentAssertions. Property tests: FsCheck.
- Name: `MethodName_StateUnderTest_ExpectedBehavior`.
- Arrange / Act / Assert, with those comments.
- `async Task` only. Never `async void`.
- Assert with `Should()`.
- Fake `IOnnxEncoderSession` and `IZVecCollection`. If a test needs the real native library, it is an integration test; move it.
- Cover `Recall.Core`. Gate: about 70% line coverage at v1, 60% at the M1 exit. Do not chase coverage with tests of generated code or the MAUI host.

## What must have unit tests

- MRL policy: age, pin, and last access. FsCheck invariants: dimension is only 768, 256, or 128; pinned implies 768; promote-on-access never lowers a tier; anything under 128 throws.
- Modality router: extension and mime to encoder mode. Audio on a mobile policy returns skip, not the 740M mode.
- Scope overlap normalization.
- Content-addressable id: same bytes and tier, same id; different tier, different id.
- RRF fusion of two ranked lists, including the outer tier fusion, with `k=60` fixed in the test name's expected values.
- Task-prefix selection from Find / Group / Tag.
- Deterministic labeler: stop-words dropped, short tokens dropped, empty topic allowed, locale not hardcoded to English in the assertion helper.
- Op-log append and torn-tail discard. No socket tests.

## Seams

If production code cannot be tested without ONNX or ZVec, add the interface in Core rather than marking the test `[Fact(Skip=...)]`. Do not put UI tests here.

## Review stance

Block tests that download a model, open the network, or read the developer's real home folder. Amend docs that still tell M1 to integration-test clipboard capture.
