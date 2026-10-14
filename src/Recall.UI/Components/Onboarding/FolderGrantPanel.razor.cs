using Microsoft.AspNetCore.Components;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.UI.Components;

public partial class FolderGrantPanel
{
    [Inject]
    private IFolderPickerService Picker { get; set; } = null!;

    [Inject]
    private IRecallShellKind Shell { get; set; } = null!;

    [Parameter]
    public int Step { get; set; } = 1;

    [Parameter]
    public bool ShowStepper { get; set; }

    [Parameter]
    public bool ShowNavButtons { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback OnContinue { get; set; }

    private string? overlapWarning;
    private string? permissionError;

    private async Task AddFolderAsync()
    {
        overlapWarning = null;
        permissionError = null;
        FolderPickerResult result;
        try
        {
            result = await Picker.PickFolderAsync();
        }
        catch (Exception)
        {
            permissionError = T("Folders_PermissionDenied");
            return;
        }
        if (result.Outcome == FolderPickerOutcome.Denied)
        {
            permissionError = T("Folders_PermissionDenied");
            return;
        }

        if (result.Outcome != FolderPickerOutcome.Selected || string.IsNullOrWhiteSpace(result.Path))
        {
            return;
        }

        var name = DisplayNameFor(result.Path);

        var add = Ui.TryAddGrant(new ScopeFolderGrant(result.Path, name, GrantFileKinds.All, result.ReopenToken));
        if (add.Status == ScopeGrantAddStatus.OverlapChildOfExistingParent)
        {
            overlapWarning = T("Folders_OverlapWarning");
        }
    }

    private void Remove(string path)
    {
        Ui.RemoveGrant(path);
        overlapWarning = null;
        permissionError = null;
    }

    private bool ShowIosNote => Shell.Kind == RecallShellKind.Ios;

    private void ToggleKind(string path, GrantFileKinds kind, bool enabled)
    {
        var grant = Ui.Current.Grants.FirstOrDefault(g => string.Equals(g.Path, path, StringComparison.OrdinalIgnoreCase));
        if (grant is null)
        {
            return;
        }

        var next = enabled ? grant.EnabledKinds | kind : grant.EnabledKinds & ~kind;
        if (next == GrantFileKinds.None)
        {
            return;
        }

        Ui.SetGrantFileKinds(path, next);
    }

    private static bool IsKindEnabled(GrantFileKinds kinds, GrantFileKinds kind) => kinds.HasFlag(kind);

    private static string PathLabel(string path)
    {
        var tree = TreeLabel(path);
        return string.IsNullOrWhiteSpace(tree) ? path : tree;
    }

    private static string DisplayNameFor(string path)
    {
        var tree = TreeLabel(path);
        if (!string.IsNullOrWhiteSpace(tree))
        {
            var colon = tree.LastIndexOf(':');
            if (colon >= 0 && colon < tree.Length - 1)
            {
                return tree[(colon + 1)..];
            }

            return tree;
        }

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var file = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(file) ? path : file;
    }

    private static string? TreeLabel(string path)
    {
        if (!path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var decoded = Uri.UnescapeDataString(path);
        const string marker = "/tree/";
        var index = decoded.LastIndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var tree = decoded[(index + marker.Length)..];
        var slash = tree.IndexOf('/');
        if (slash >= 0)
        {
            tree = tree[..slash];
        }

        return tree;
    }
}
