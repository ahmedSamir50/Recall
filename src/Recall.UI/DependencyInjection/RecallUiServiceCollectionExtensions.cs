using Microsoft.Extensions.DependencyInjection;
using Recall.UI.State;

namespace Recall.UI.DependencyInjection;

public static class RecallUiServiceCollectionExtensions
{
    public static IServiceCollection AddRecallUi(this IServiceCollection services)
    {
        services.AddSingleton<ShellUiStore>();
        return services;
    }
}
