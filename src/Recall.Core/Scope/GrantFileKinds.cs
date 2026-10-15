namespace Recall.Core.Scope;

[Flags]
public enum GrantFileKinds
{
    None = 0,
    Text = 1,
    Image = 2,
    Video = 4,
    Audio = 8,
    Code = 16,
    All = Text | Image | Video | Audio | Code,
}
