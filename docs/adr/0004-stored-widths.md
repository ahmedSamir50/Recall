Status: accepted, 15 Oct 2026.

Context: Matryoshka cuts had to be one policy.

Decision: stored widths are 768, 512, and 256. 128 is rejected. Text is stored at 512 until a golden run clears 256. Image, video, and audio start at 768 when those collections exist.

Consequences: `EnsureStorableWidth` is the code gate for text and for 128.
