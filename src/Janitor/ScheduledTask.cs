using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Janitor;

public class ScheduledTask
{
    private readonly Delegate _taskToBeScheduled;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISchedule _schedule;
    private readonly Delegate _stateHandler;
    private readonly ILogger _logger;
    private CancellationTokenSource _cancellationTokenSource;

    public ScheduledTask(string name, Delegate taskToBeScheduled, IServiceProvider serviceProvider, ISchedule schedule, Delegate stateHandler, ILogger logger)
    {
        Name = name;
        _taskToBeScheduled = taskToBeScheduled;
        _serviceProvider = serviceProvider;
        _schedule = schedule;
        _stateHandler = stateHandler;
        _logger = logger;
        State = TaskState.ScheduleRequested;
    }

    private Task? _delayedTask;

    public string Name { get; }

    public TaskState State { get; private set; }

    public async Task SetState(TaskState newState, Exception? exception = null)
    {
        _logger.LogDebug("Changing state from '{State}' to '{newState}' for task '{Name}'", State, newState, Name);

        State = newState;
        try
        {
            await InvokeStateHandler(exception);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke state handler for task '{taskName}'", Name);
        }

    }

    private async Task InvokeStateHandler(Exception? exception)
    {
        var handlerParameters = _stateHandler.Method.GetParameters();
        List<object?> args = new List<object?>();
        using (var scope = _serviceProvider.CreateScope())
        {
            foreach (var handlerParameter in handlerParameters)
            {
                if (handlerParameter.ParameterType == typeof(Exception))
                {
                    args.Add(exception);
                }
                else
                if (handlerParameter.ParameterType == typeof(string))
                {
                    args.Add(Name);
                }
                else
                if (handlerParameter.ParameterType == typeof(TaskState))
                {
                    args.Add(State);
                }
                else
                {
                    var argument = scope.ServiceProvider.GetRequiredService(handlerParameter.ParameterType);
                    args.Add(argument);
                }
            }
        }

        await (Task)_stateHandler.DynamicInvoke(args.ToArray())!;
    }

    public Task GetTask()
    {
        if (_delayedTask is null)
        {
            _delayedTask = CreateScheduledTask();
        }

        return _delayedTask;
    }


    private async Task CreateScheduledTask()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            TimeSpan? waitTime = GetWaitTime();

            try
            {
                if (waitTime is not null)
                {
                    await LongDelay((long)waitTime.Value.TotalMilliseconds, _cancellationTokenSource.Token);
                    if (State == TaskState.Paused)
                    {
                        _logger.LogDebug("Task '{Name}' is in a paused state. To resume the task, call 'Resume{Name}'", Name, Name);
                    }
                    else
                    {
                        var handlerParameters = _taskToBeScheduled.Method.GetParameters();
                        List<object> args = new List<object>();
                        using (var containerScope = _serviceProvider.CreateScope())
                        {
                            foreach (var handlerParameter in handlerParameters)
                            {
                                if (handlerParameter.ParameterType == typeof(CancellationToken))
                                {
                                    args.Add(_cancellationTokenSource.Token);
                                }
                                else
                                {
                                    var argument = containerScope.ServiceProvider.GetRequiredService(handlerParameter.ParameterType);
                                    args.Add(argument);
                                }

                            }
                            await SetState(TaskState.Running);
                            await (Task)_taskToBeScheduled.DynamicInvoke(args.ToArray())!;
                            await SetState(TaskState.Scheduled);
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("The task '{taskName}' is no longer scheduled and will be deleted", Name);
                    await SetState(TaskState.DeleteRequested);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                if (State == TaskState.StopRequested)
                {
                    _logger.LogDebug("The task '{Name}' was stopped/cancelled", Name);
                    _delayedTask = null;
                    await SetState(TaskState.Stopped);
                }
                if (State == TaskState.DeleteRequested)
                {
                    _logger.LogDebug("Delete requested for task '{Name}'. It will be removed when the main task is recreated.", Name);
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute background task '{taskName}'", Name);
                await SetState(TaskState.Failed, ex);
            }
        }
    }

    static async Task LongDelay(long delay, CancellationToken cancellationToken)
    {
        while (delay > 0)
        {
            var currentDelay = delay > int.MaxValue ? int.MaxValue : (int)delay;
            await Task.Delay(currentDelay, cancellationToken);
            delay -= currentDelay;
        }
    }

    private TimeSpan? GetWaitTime()
    {
        var utcNow = DateTime.UtcNow;
        DateTime? nextExecutionTime = _schedule.GetNext(utcNow);

        if (nextExecutionTime is null)
        {
            return null;
        }

        TimeSpan waitTime = nextExecutionTime.Value - utcNow;
        _logger.LogDebug($"Scheduled {Name} for execution at {nextExecutionTime} (UTC). Time to wait is {waitTime.Days} day(s), {waitTime.Hours} hour(s), {waitTime.Minutes} minute(s) and {waitTime.Seconds} second(s).");
        return waitTime;
    }

    public async Task Stop()
    {
        await SetState(TaskState.StopRequested);
        _cancellationTokenSource.Cancel();
    }

    public async Task Delete()
    {
        await SetState(TaskState.DeleteRequested);
        _cancellationTokenSource.Cancel();
    }

    public async Task Pause()
    {
        await SetState(TaskState.Paused);
    }

    public async Task Resume()
    {
        await SetState(TaskState.Scheduled);
    }
}