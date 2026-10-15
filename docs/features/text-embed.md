Status: in-code

## Policy

`RecallIndexPolicy.TextStoredWidth` is `W512`. `EnsureStorableWidth` throws `StoredWidthBlocked` when width is 128, and `TextWidthNot512` when modality is text and width is not 512.

## Cut

`MatryoshkaVectorMath.CutAndNormalize` keeps the leading stored width and L2-normalizes that slice. Search and ingest each embed once, then cut to 512. `OnnxTextEmbeddingEncoder.EmbedPrefixedTextAsync` throws on blank text. It takes a `SemaphoreSlim(1, 1)` around the run. It requires graph inputs `input_ids`, `attention_mask`, `image_features`, `video_features`, and `audio_features`, or throws `InvalidOperationException` with message `Text model inputs are missing.` A feature input that is not float throws `InvalidOperationException` with message `Feature input must be float.` Image, video, and audio features are empty float tensors from `EmptyFeatureDimensions`. The output whose name contains `sentence` is preferred, otherwise the first output. A vector that is not already 768 wide is mean-pooled, then L2-normalized.

## Prefixes

Search `task: search result | query: `. Cluster `task: clustering | query: `. Classify `task: classification | query: `. Titled `title: {title} | text: {content}`. Empty title `text: {content}`. `WrapSearchQuery` throws `SearchPrefixAppliedTwice` when the query already starts with the search prefix, and throws on blank. `WrapTitledDocument` throws `DocumentPrefixAppliedTwice` when content already has the title marker and ` | text: `, and throws on blank content. Cluster and classify methods are not called by `FindAsync`. Pictures, video, and audio have no prefix code because those encoders are absent.
