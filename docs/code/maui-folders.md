## src/Recall.Maui/Platform/Folders/MauiFolderPickerService.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiFolderPickerService : IFolderPickerService
    public Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default) =>
```

No branch.


## src/Recall.Maui/Platform/Folders/MauiFolderPickerService.Android.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiFolderPickerService
```

No branch.


## src/Recall.Maui/Platform/Folders/MauiFolderPickerService.iOS.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiFolderPickerService
        public FolderPickerDelegate(TaskCompletionSource<FolderPickerResult> tcs) => this.tcs = tcs;
        public override void WasCancelled(UIDocumentPickerViewController controller) =>
        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
        if (root is null)
            if (!url.StartAccessingSecurityScopedResource())
            if (bookmark is null)
```


## src/Recall.Maui/Platform/Folders/MauiFolderPickerService.Windows.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiFolderPickerService
            if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            if (folder is null)
        catch (Exception ex) when (ex is UnauthorizedAccessException or PermissionException)
```


## src/Recall.Maui/Platform/Folders/MauiFolderPickerService.Default.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiFolderPickerService
```

No branch.


## src/Recall.Maui/Platform/Folders/AndroidFolderPickerCoordinator.cs

```csharp
namespace Recall.Maui.Platform;
    public static void BindLauncher(ActivityResultLauncher launcher)
    public static Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken)
    public static void CompleteUri(Android.Net.Uri? uri)
    public static void Complete(Result resultCode, Intent? data)
        if (activity is null || treeLauncher is null)
            catch (Exception)
        if (pending is null)
        if (uri is null)
        catch (Exception)
        if (resultCode != Result.Ok)
```

No public type.


## src/Recall.Maui/Platform/Folders/MauiGrantedFolderAccessService.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedFolderAccessService : IGrantedFolderAccessService
    public MauiGrantedFolderAccessService(IScopeGrantStore grants)
    public void RestorePersistedAccess() => RestorePersistedAccessPlatform(grants.List());
```

No branch.


## src/Recall.Maui/Platform/Folders/MauiGrantedFolderAccessService.Android.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedFolderAccessService
        if (activity is null)
            if (!path.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
```


## src/Recall.Maui/Platform/Folders/MauiGrantedFolderAccessService.iOS.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedFolderAccessService
            if (string.IsNullOrWhiteSpace(grant.ReopenToken))
```


## src/Recall.Maui/Platform/Folders/MauiGrantedFolderAccessService.Windows.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedFolderAccessService
```

No branch.


## src/Recall.Maui/Platform/Folders/MauiGrantedTextReader.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedTextReader : IGrantedTextReader
    public MauiGrantedTextReader(IGrantedFolderAccessService access)
    public IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, CancellationToken cancellationToken)
        public IAsyncEnumerator<GrantedTextDocument> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        if (Interlocked.Exchange(ref restored, 1) == 0)
        if (TryPlatform(grant, cancellationToken, out var documents))
```


## src/Recall.Maui/Platform/Folders/MauiGrantedTextReader.Android.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedTextReader
        if (!grant.Path.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
        if (resolver is null || tree is null)
        if (string.IsNullOrEmpty(rootId))
            if (children is null)
            if (cursor is null)
                if (string.Equals(mime, DocumentsContract.Document.MimeTypeDir, StringComparison.Ordinal))
                    if (id.Length > 0)
                if (!GrantTextFiles.IsTextFile(name) || id.Length == 0)
                if (documentUri is null)
                if (stream is null)
                if (!TextFileSignature.IsSafeText(sample.AsSpan(0, sampleCount)))
                if (modifiedColumn >= 0 && !cursor.IsNull(modifiedColumn))
                    if (milliseconds > 0)
```


## src/Recall.Maui/Platform/Folders/MauiGrantedTextReader.Apple.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedTextReader
        if (string.IsNullOrWhiteSpace(grant.ReopenToken))
            if (url?.Path is not string path || path.Length == 0)
            if (started)
```


## src/Recall.Maui/Platform/Folders/MauiGrantedTextReader.Desktop.cs

```csharp
namespace Recall.Maui.Platform;
public sealed partial class MauiGrantedTextReader
```

No branch.
