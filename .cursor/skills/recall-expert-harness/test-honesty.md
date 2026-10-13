# Test honesty

Tests are honest and effective, or they do not count. The verdict is the behavior under test: critical paths, hot paths, and edge cases. A test counts only when deleting that behavior makes the test fail.

Line coverage stays in the rule. The floor is 60% at M1 and 70% later. That floor is not the verdict, not the assignment, and not something you satisfy by covering easy lines. A feature at 70% still fails if a critical path, a hot path, or an edge case has no test that can go red. A feature under the floor also fails. Do not chase the percent with generated code, the MAUI host, or lines that do not decide behavior.

A critical path means every interface method and every branch that decides behavior: each `if`, `else`, `else if`, `switch` arm, `try`, and `catch`. Calling a branch “not critical” does not take it off the matrix. Hot paths are the ones a search or an ingest actually runs. Edge cases include null, empty, cancellation, the discarded `switch` arm, and each `catch`.

## What must be tested

- Every method on a new or changed interface has at least one test. An interface with three methods needs tests that call all three. A test that only calls one method does not cover the interface.
- Every branch in the method under test has a test that forces it: `if`, `else`, `else if`, `switch` (each arm, including the discard arm), `try` success, and `catch` for each exception type the method handles. `finally` cleanup is asserted when it releases a handle, a watcher, or a transaction.
- Null, empty, and cancellation paths are branches when the code tests them.
- A `switch` on an enum uses the enum from the single source of truth, not a copied string.

## Who writes which tests

| Behavior | Project | Expert |
|---|---|---|
| Pure Core logic, fakes for ONNX and ZVec | `tests/Recall.UnitTests` | `unit-tests` |
| Real ONNX + real ZVec, crash restart, egress | `tests/Recall.IntegrationTests` | `retrieval-qa` |
| Golden recall@k | `tests/Recall.CorpusTests` | `retrieval-qa` |
| Latency and memory gates | `src/Recall.Benchmarks` | `performance` |

Do not satisfy the matrix by marking tests `Skip`, by asserting `NotNull` only, or by one theory that never varies the branch input.

## Plans

A `@recall:` plan that adds a type or a method must name the test for each method and each branch, or `unit-tests` returns `push-back`. "Add unit tests" is not a test plan.

If the plan has no code yet, the sign-off still lists the methods and branches the design implies. Missing catch behavior, missing else behavior, and a public method with no test are gaps.

## Honesty failures

Treat these as a block from `unit-tests` or `retrieval-qa`:

- A test that cannot fail if the production branch is deleted.
- Asserting the stub return instead of the branch result.
- Covering only the happy path of a method that catches and swallows.
- Treating 60% or 70% as the verdict, or calling a feature done because the report cleared the floor while a critical path, hot path, or edge case has no honest test.
- Dropping the 60% / 70% floor. The floor stays. It is not the homework and it is not enough.
- Adding tests of generated code, the MAUI host, or a line that is not a critical path, in order to move a percentage.
- Putting a real model download or the developer's home folder in a unit test.
