using Microsoft.AspNetCore.Components;

namespace Recall.UI.Components;

public partial class OnboardingHeader
{
    [Parameter]
    public int Step { get; set; }
}
