using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Janitor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJanitor(this IServiceCollection services, Action<IServiceProvider, ITaskRunner> config)
    {
        // var backgroundTaskTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IBackgroundTask))).ToArray();

        // foreach (var backgroundTaskType in backgroundTaskTypes)
        // {
        //     services.AddScoped(backgroundTaskType);
        // }
        // services.AddSingleton<ICronExpressionProvider, CronExpressionProvider>();
        services.AddSingleton<ITaskRunner>(sp =>
        {
            // var cronExpressionProvider = sp.GetService<ICronExpressionProvider>();
            var scheduler = new TaskRunner(sp, sp.GetRequiredService<ILogger<TaskRunner>>());
            config(sp, scheduler);

            // foreach (var backGroundTaskType in backgroundTaskTypes)
            // {
            //     scheduler.Schedule(backGroundTaskType.Name, (sp, ct) =>
            //     {
            //         using (var scope = sp.CreateScope())
            //         {
            //             var instance = (IBackgroundTask)sp.GetService(backGroundTaskType);
            //             return instance.ExecuteAsync(ct);
            //         }

            //     }, cronExpressionProvider.GetCronExpression(backGroundTaskType));
            // }

            return scheduler;
        });

        // services.AddHostedService<BackgroundTasksHostedService>();

        return services;
    }
}