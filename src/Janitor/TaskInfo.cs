using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Janitor;

public interface ITaskInfo
{
    TaskState State { get; set; }

    Task GetScheduledTask(ILogger<ITaskRunner> logger);

    Task SetState(TaskState newState);

    Task Pause();
}

 

public class TaskInfo<TDependency> : ITaskInfo where TDependency : notnull
{

    public Delegate StateHandler { get; set; }

    private Task _scheduledTask;

    public string Name { get; set; }

    public Func<TDependency, CancellationToken, Task> TaskToBeScheduled { get; set; }

    public CancellationTokenSource CancellationTokenSource { get; set; }

    public ISchedule Schedule { get; set; }

    public TaskState State { get; set; } = TaskState.StartRequested;

    public IServiceProvider ServiceProvider { get; set; }

    public async Task SetState(TaskState newState)
    {
        State = newState;
        try
        {
            await InvokeStateHandler();
        }
        catch (System.Exception ex)
        {

            //TODO 
        }

    }

    private async Task InvokeStateHandler()
    {
        var handlerParameters = StateHandler.Method.GetParameters();
        List<object> args = new List<object>();
        using (var scope = ServiceProvider.CreateScope())
        {
            foreach (var handlerParameter in handlerParameters)
            {
                var argument = scope.ServiceProvider.GetRequiredService(handlerParameter.ParameterType);
                args.Add(argument);
            }
        }

        await (Task)StateHandler.DynamicInvoke(args.ToArray());
    }

    public Task GetScheduledTask(ILogger<ITaskRunner> logger)
    {
        if (_scheduledTask is null)
        {
            _scheduledTask = CreateScheduledTask(logger);
        }

        return _scheduledTask;
    }


    private async Task CreateScheduledTask(ILogger<ITaskRunner> logger)
    {
        this.CancellationTokenSource = new CancellationTokenSource();
        while (!CancellationTokenSource.Token.IsCancellationRequested)
        {
            TimeSpan? waitTime = GetWaitTime(logger);

            try
            {
                if (waitTime is not null)
                {
                    await Task.Delay(waitTime.Value, CancellationTokenSource.Token);
                    if (State == TaskState.Paused)
                    {
                        logger.LogDebug($"Task `{Name}` is in a paused state. To resume the task, call `Resume{Name}`");
                    }
                    else
                    {
                        using (var containerScope = ServiceProvider.CreateScope())
                        {
                            var dependency = containerScope.ServiceProvider.GetRequiredService<TDependency>();
                            await TaskToBeScheduled(dependency, CancellationTokenSource.Token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute background task");
            }
        }
        //};
        // if (taskInfo.State == TaskState.StopRequested)
        // {
        //     _logger.LogDebug("Stop is requested");
        //     taskInfo.State = TaskState.Stopped;
        //     _restartCompletionSource.SetResult();
        // }
    }

    private TimeSpan? GetWaitTime(ILogger<ITaskRunner> logger)
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime? nextExecutionTime = Schedule.GetNext(utcNow);

        if (nextExecutionTime is null)
        {
            return null;
        }

        TimeSpan waitTime = nextExecutionTime.Value - utcNow;
        logger.LogDebug($"Scheduled {Name} for execution at {nextExecutionTime} (UTC). Time to wait is {waitTime.Days} day(s), {waitTime.Hours} hour(s), {waitTime.Minutes} minute(s) and {waitTime.Seconds} second(s).");
        return waitTime;
    }

    public async Task Pause()
    {
        await SetState(TaskState.Paused);
    }
}






// /// <summary>
// /// Represents the task to be scheduled.
// /// </summary>
// /// <param name="Name">The unique name of the task.</param>
// /// <param name="Task">The <see cref="Task"/> to be scheduled.</param>
// /// <param name="scheduler">The IWaitTime </param>
// // public record TaskInfo(string Name, Func<TaskInfo, CancellationToken, Task> Task, Func<TaskInfo, CancellationToken, Task> StateHandler)
// public record TaskInfo(string Name, Func<TaskInfo, CancellationToken, Task> Task)
// {
//     /// <summary>
//     /// Gets or sets the <see cref="CancellationTokenSource"/> that is used to cancel the task.
//     /// </summary>
//     public CancellationTokenSource? CancellationTokenSource { get; set; }

//     /// <summary>
//     /// Gets or sets the current state of the task.
//     /// </summary>
//     public TaskState State { get; private set; } = TaskState.StartRequested;

//     public async Task SetState(TaskState newState)
//     {
//         State = newState;
//         // await StateHandler(this, CancellationTokenSource!.Token);
//     }
// }
