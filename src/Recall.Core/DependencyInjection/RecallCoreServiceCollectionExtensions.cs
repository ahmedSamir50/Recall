using Microsoft.Extensions.DependencyInjection;
using Recall.Core.Locale;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Theme;

namespace Recall.Core.DependencyInjection;

public static class RecallCoreServiceCollectionExtensions
{
    public static IServiceCollection AddRecallCore(this IServiceCollection services)
    {
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LocaleService>();
        services.AddSingleton<IScopeManagerService, ScopeGrantService>();
        return services;
    }
}
