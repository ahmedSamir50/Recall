# Contributing to Recall

Thanks for looking inside. Recall is an on-device second brain: the folders a person trusts, searchable, with the index never leaving the device. A contribution has to keep that promise.

## The rule that matters most

The task list is [docs/plan.md](docs/plan.md). It is the only progress table.

- A bug report should name the task id the bug belongs to, for example `A.2.6`.
- A feature request is accepted only when it names a task id of `docs/plan.md` or argues for a new task in that plan.
- A pull request carries its task id in the title or body. One task per pull request.

## How to build

- .NET 10 SDK plus the MAUI workloads. `global.json` pins the SDK.
- ZVec.NET 1.0.0-beta.6 is not on nuget.org. Restore uses the local feed in your user NuGet config. This repository does not publish that package.
- `dotnet build Recall.slnx` builds the solution. `dotnet test` runs the suites.
- `git config core.hooksPath .githooks` once per clone strips accidental `Co-authored-by: Cursor` trailers from commit messages.
- A banned-API analyzer rejects `HttpClient`, `Socket`, `TcpListener`, and `UdpClient` inside `Recall.Core`.

## Tests

Read [docs/testing.md](docs/testing.md) before writing any. The short version: a test counts only if deleting the behavior makes it fail. Do not mark tests `Skip`, do not assert `NotNull` only, and never raise a coverage percent with tests of generated code or the host. Every new interface method and every branch that decides behavior needs a test that can go red.

## Product copy

Every string a person can see is in Arabic and English resource files, including empty states and errors. If your change adds visible text, both `.resx` files get the sentence. Arabic layout is right to left. Repo copy such as this file and the README can be English only.

## What will not be merged, even if proposed

- Anything that sends user content, vectors, or identifiers off the device. The model download is the only network path in the app.
- A speech-to-text model, a chat UI, a generative summary, a helper process.
- Clipboard, camera, screen, or microphone capture in v1.
- A stored vector at 128 dimensions or fewer.
- Claims in the README about a capability that is not in the tree, or an install row for a release that does not exist.

## License

By contributing you agree your work is covered by the repository [LICENSE](LICENSE).
