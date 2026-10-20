<p align="center">
  <img src="src/Recall.UI/wwwroot/brand/infinity-icon-small-dark.svg" alt="Recall" width="96" />
</p>

# Recall

**The folders you trust, searchable. The index never leaves the device.**

Recall is an on-device second brain. You pick the paths and the file types. It embeds what you allowed, and Find runs in that same process. No account. No cloud copy of your files. No hosted vector database.

## What it can do

This is in the app today.

- Grant a folder and pick the file types. Code can be one of those types.
- Leave secrets, databases, lock files, and the built-in skip folders unread. You can add a skip. You cannot remove a built-in one.
- Download the text model, check SHA-256, and embed granted text.
- Find by meaning and by the words, then open the file here.
- Switch the screens between Arabic and English.
- Name this device, and store an age cutoff of 2, 5, or 7 years. Sync stays off.
- On iOS, pick up new files when you open Recall. Other shells watch the granted folders.

Granted files may use other languages. A scored recall report is not in this tree yet.

## What v1 will do

This is the v1 product. Not all of this is in the tree yet.

- Search pictures, sampled video, and audio files when that type is on. There is no microphone.
- Read the words in a picture, and attach a transcript that already sits next to the media. There is no speech-to-text model.
- Pin an item so it stays easy to find.
- After 2, 5, or 7 years, let older pictures, video, and audio match on a shorter vector. Text does not age.
- Ship installers for Android, iPhone, Windows, and Mac. The weights are downloaded, not packed inside.

## Where it runs

v1 RIDs are `android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`, and `linux-x64`.

Builds today: `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, and `net10.0-windows10.0.19041.0`. .NET MAUI has no Linux target framework. The Linux shell is not in the tree. `linux-arm64` and `win-arm64` are not v1 RIDs.

## The future

After v1. No date is attached to these.

- A Linux shell, once a supported host exists. The experimental GTK4 packages are not how v1 gets there.
- A tray, start at sign-in, and a search shortcut. They are open, and they are not in the tree.
- Sync is designed and off. Pairing is not in v1.
- An opt-in local notes model in v1.5. It stays on this device. v1 has no language model, no chat, and no generated summary. Search still works while notes are off. That model is not the text embedder. Notes stay off on iPhone while that library leaves iOS unfinished.

## Not this product

No web app. No clipboard, camera, screen, or microphone capture. Your files are not the training set.

## Docs

Latest App Docs start at [docs/index.md](docs/index.md). `InitDocs/` is the initial idea, not the spec.

## Build

Build and release stay on the developer machine. This repository has no GitHub Actions workflow.

The text class is LlamaTextEmbeddingEncoder, in-process llama.cpp. ZVec.NET 1.0.0-beta.6 stays in that same process. It is not on nuget.org. Restore uses the local feed in the user NuGet config. This repository does not publish that package.

## License

The license is [LICENSE](LICENSE). It is source-available. A commercial use is not granted.
