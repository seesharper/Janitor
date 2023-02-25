using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Janitor;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Janitor to the IServiceCollection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddJanitor(this IServiceCollection services, Action<IServiceProvider, IJanitor>? config = null)
    {
        services.AddSingleton<IJanitor>(sp =>
        {
            var janitor = new Janitor(sp, sp.GetRequiredService<ILogger<Janitor>>());
            config?.Invoke(sp, janitor);
            return janitor;
        });

        return services;
    }
}