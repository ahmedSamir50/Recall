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
- Cover `Recall.Core` honestly. Follow [test-honesty.md](../recall-expert-harness/test-honesty.md). An interface with three methods needs tests that exercise all three. Each `if`, `else`, `else if`, `switch` arm, `try`, and `catch` that decides behavior needs a test that forces that path and fails if that path is deleted. Also cover the hot path and the edge cases. The floor is 60% at M1 and 70% later. Clearing the floor is not the verdict. Do not add tests only to move the percentage.

## What must have unit tests

- Width policy: a stored width of 128 or below is rejected. Text is 512 until a golden run clears 256. Old image, video, and audio move to 512. A pin stays at the long width. The cutoff is only 2, 5, or 7 years, starting at 2. Test each of those three and reject any other value.
- Modality router: extension and mime to encoder mode. Audio is a separate encoder, loaded only when that file type is enabled, then unloaded. There is no microphone path.
- Scope overlap normalization.
- Content-addressable id: same bytes and tier, same id; different tier, different id.
- RRF fusion of two ranked lists, including the outer tier fusion, with `k=60` fixed in the test name's expected values.
- Task-prefix selection from Find / Group / Tag.
- Deterministic labeler: stop-words dropped, short tokens dropped, empty topic allowed, locale not hardcoded to English in the assertion helper.
- Op-log append and torn-tail discard. No socket tests.

## Seams

If production code cannot be tested without ONNX or ZVec, add the interface in Core rather than marking the test `[Fact(Skip=...)]`. Do not put UI tests here.

## Review stance

Block a plan or a change that leaves a method or a branch untested, and block tests that download a model, open the network, or read the developer's real home folder. A happy-path-only test is a push-back, not a pass. Amend docs that still tell M1 to integration-test clipboard capture.

## Panel duty

A `@recall:` or `@reacall:` plan convenes you with the full panel. Read [development-rules.md](../recall-expert-harness/development-rules.md), [test-honesty.md](../recall-expert-harness/test-honesty.md), and [sign-off.md](../recall-expert-harness/sign-off.md). Research the plan against this skill, the canon, and the repo. Push back when the plan, the user, or InitDocs is wrong, inconsistent, or missing a risk. Agree only after a checked list. Return the sign-off. Do not rubber-stamp. A plan that says "add tests" without a method and branch matrix is `push-back`.
