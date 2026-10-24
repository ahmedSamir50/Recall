# ADR 0015: Proof hosts

Status: accepted, 24 Oct 2026.

## Context

U6 left GitHub Actions open, and several done-when lines required a physical Apple run.

## Decision

Windows, Linux in WSL or Docker, and Android on an emulator or a phone over adb are the local proof hosts. ios-arm64 and maccatalyst-arm64 stay in the solution and compile on a GitHub-hosted macOS runner. A physical iPhone or Mac run is not a done-when. Platform code for those heads is still written.

## Consequences

ADR 0013’s “do not add Actions” sentence is superseded for this one workflow. ADR 0014’s “no Linux shell” sentence stays. No signing certificate. Implementation stays in its task.
