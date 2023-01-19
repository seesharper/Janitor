using System.Collections;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Janitor;


public class TaskInfoBuilder<TDependency> where TDependency : notnull
{
    private string _name;
    private Func<TDependency, CancellationToken, Task> _taskToBeScheduled;
    private ISchedule _schedule;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ITaskRunner> _logger;

    public TaskInfoBuilder(IServiceProvider serviceProvider, ILogger<ITaskRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public TaskInfoBuilder<TDependency> WithName(string name)
    {
        _name = name;
        return this;
    }

    public TaskInfoBuilder<TDependency> WithSchedule(ISchedule schedule)
    {
        _schedule = schedule;
        return this;
    }

    public TaskInfoBuilder<TDependency> WithScheduledTask(Func<TDependency, CancellationToken, Task> task)
    {
        _taskToBeScheduled = task;
        return this;
    }

    public TaskInfo Build()
    {
        Func<TaskInfo, CancellationToken, Task> scheduledTask = async (taskInfo, cancellationToken) =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan? waitTime = GetWaitTime();

                try
                {
                    if (waitTime is not null)
                    {
                        await Task.Delay(waitTime.Value, cancellationToken);
                        if (taskInfo.State == TaskState.Paused)
                        {
                            _logger.LogDebug($"Task `{taskInfo.Name}` is in a paused state. To resume the task, call `Resume{taskInfo.Name}`");
                        }
                        else
                        {
                            using (var containerScope = _serviceProvider.CreateScope())
                            {
                                var dependency = containerScope.ServiceProvider.GetRequiredService<TDependency>();
                                await _taskToBeScheduled(dependency, cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute background task");
                }
            }
        };
        return new TaskInfo(_name, scheduledTask);
    }

    private TimeSpan? GetWaitTime()
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime? nextExecutionTime = _schedule.GetNext(utcNow);

        if (nextExecutionTime is null)
        {
            return null;
        }

        TimeSpan waitTime = nextExecutionTime.Value - utcNow;
        _logger.LogDebug($"Scheduled {_name} for execution at {nextExecutionTime} (UTC). Time to wait is {waitTime.Days} day(s), {waitTime.Hours} hour(s), {waitTime.Minutes} minute(s) and {waitTime.Seconds} second(s).");
        return waitTime;
    }
}





public class TaskRunner : ITaskRunner
{
    private readonly ConcurrentDictionary<string, TaskInfo> _scheduledTasks = new ConcurrentDictionary<string, TaskInfo>();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskRunner> _logger;
    private TaskCompletionSource _restartCompletionSource = new();

    private Func<TaskInfo, IServiceProvider, Task>? _stateChanged = null;

    public TaskRunner(IServiceProvider serviceProvider, ILogger<TaskRunner> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ITaskRunner WithStateChangedHandler(Func<TaskInfo, IServiceProvider, Task> handler)
    {
        _stateChanged = handler;
        return this;
    }

    // public ITaskRunner Schedule(string name, Func<CancellationToken, Task> task, ISchedule waitTime)
    // {


    //     Func<IServiceProvider, CancellationToken, Task> t = (sp, ct) => task(ct);
    //     var scheduledTaskInfo = new TaskInfo(name, t, waitTime);
    //     _scheduledTasks.AddOrUpdate(name, sti => scheduledTaskInfo, (n, sti) => sti);
    //     _restartCompletionSource.SetResult();
    //     return this;
    // }



    public async Task Start(CancellationToken cancellationToken)
    {
        await CreateMainTask(cancellationToken);
        _logger.LogDebug("Exiting taskrunner");
    }

    private async Task CreateMainTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            List<Task> existingTasks = _scheduledTasks.Values.Where(v => v.State is TaskState.Running || v.State == TaskState.Started || v.State == TaskState.Paused).Select(ti => ti.Task(ti, ti.CancellationTokenSource!.Token)).ToList();
            List<TaskInfo> allNewTasks = new List<TaskInfo>();
            var tasksToBeScheduled = _scheduledTasks.Values.Where(v => v.State is TaskState.StartRequested);
            _logger.LogInformation($"Creating main task with {tasksToBeScheduled.Count()} tasks.");
            foreach (var scheduledTaskInfo in tasksToBeScheduled)
            {
                scheduledTaskInfo.CancellationTokenSource = new CancellationTokenSource();
                await scheduledTaskInfo.SetState(TaskState.Started);
                allNewTasks.Add(scheduledTaskInfo);
            }


            _restartCompletionSource = new TaskCompletionSource();
            existingTasks.Add(_restartCompletionSource.Task);
            await Task.WhenAny(existingTasks);
        }
    }

    public async Task StopTask(string name)
    {
        await _scheduledTasks[name].SetState(TaskState.StopRequested);
        _scheduledTasks[name].CancellationTokenSource!.Cancel();
    }

    public async Task Pause(string taskName)
    {
        await _scheduledTasks[taskName].SetState(TaskState.Paused);
    }

    public async Task Resume(string taskName)
    {
        await _scheduledTasks[taskName].SetState(TaskState.Started);
    }

    public async Task Delete(string taskName)
    {
        _scheduledTasks.Remove(taskName, out var task);
        //_runningTasks[taskName].CancellationTokenSource.Cancel();
    }

    public void Add(string taskName, Func<CancellationToken, Task> task, string cronExpression)
    {
        throw new NotImplementedException();
    }

    public async Task Start(string taskName)
    {
        await _scheduledTasks[taskName].SetState(TaskState.StartRequested);
        _restartCompletionSource.SetResult();
    }

    public async Task Stop(string taskName)
    {
        _logger.LogDebug($"Requesting task `{taskName}` to be stopped");
        _scheduledTasks[taskName].SetState(TaskState.StopRequested);
        _scheduledTasks[taskName].CancellationTokenSource.Cancel();
        await ChangeState(_scheduledTasks[taskName]);
    }


    public IEnumerator<TaskInfo> GetEnumerator() => _scheduledTasks.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _scheduledTasks.Values.GetEnumerator();

    private async Task ChangeState(TaskInfo taskInfo)
    {
        if (_stateChanged != null)
        {
            await _stateChanged(taskInfo, _serviceProvider);
        }
    }

    public ITaskRunner Schedule<TDependency>(Action<TaskInfoBuilder<TDependency>> configureBuilder) where TDependency : notnull
    {
        var taskInfoBuilder = new TaskInfoBuilder<TDependency>(_serviceProvider, _logger);
        configureBuilder(taskInfoBuilder);
        var taskInfo = taskInfoBuilder.Build();
        _scheduledTasks.AddOrUpdate(taskInfo.Name, n => taskInfo, (n, t) => taskInfo);
        _restartCompletionSource.SetResult();
        return this;
    }
}