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
        var result = await Picker.PickFolderAsync();
        if (result.Outcome == FolderPickerOutcome.Denied)
        {
            permissionError = T("Folders_PermissionDenied");
            return;
        }

        if (result.Outcome != FolderPickerOutcome.Selected || string.IsNullOrWhiteSpace(result.Path))
        {
            return;
        }

        var name = Path.GetFileName(result.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = result.Path;
        }

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
}
