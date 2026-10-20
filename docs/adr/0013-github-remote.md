Status: accepted, 21 Oct 2026.

Context: U6 closed the remote as Bitbucket on 13 Oct 2026. The public repository https://github.com/ahmedSamir50/Recall held one README commit. Build and release stay on the developer machine.

Decision: the git remote for main is https://github.com/ahmedSamir50/Recall. The branch backup/pre-strip-embeddinggemma is not pushed. GitHub Actions is not added. No cloud build.

Consequences: ADR 0010 keeps its other cuts. Task 7.2.1 still produces the four shells on the developer machine. A later plan has to accept a cloud build before a workflow file is added. ZVec.NET 1.0.0-beta.6 stays off nuget.org, and this decision does not pick a feed.
