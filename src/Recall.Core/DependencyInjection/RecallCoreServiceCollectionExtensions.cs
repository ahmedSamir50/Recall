using Microsoft.Extensions.DependencyInjection;

namespace Recall.Core.DependencyInjection;

public static class RecallCoreServiceCollectionExtensions
{
    public static IServiceCollection AddRecallCore(this IServiceCollection services)
    {
        return services;
    }
}
