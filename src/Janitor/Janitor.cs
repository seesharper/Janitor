using System.Collections;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Janitor;

/// <summary>
/// The actual Janitor that handles scheduled tasks.
/// </summary>
public class Janitor : IJanitor
{
    private readonly ConcurrentDictionary<string, ScheduledTask> _scheduledTasks = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Janitor> _logger;
    private TaskCompletionSource _restartCompletionSource = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Janitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to start new container scopes 
    /// and resolving task/state handler dependencies.</param>
    /// <param name="logger">The logger used to log debug messages.</param>
    public Janitor(IServiceProvider serviceProvider, ILogger<Janitor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Start(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() =>
        {
            _logger.LogDebug("Main task has been cancelled.");
            foreach (var item in _scheduledTasks)
            {
                item.Value.Stop().GetAwaiter().GetResult();
            }
            if (_restartCompletionSource.Task.Status != TaskStatus.RanToCompletion)
            {
                _restartCompletionSource.SetCanceled();
            }
        });
        await CreateMainTask(cancellationToken);
        _logger.LogDebug("Exiting task runner");
    }

    private async Task CreateMainTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await RemovedDeletedTasks();
            List<Task> existingTasks = _scheduledTasks.Values.Where(v => v.State is TaskState.Running || v.State == TaskState.Scheduled || v.State == TaskState.Paused).Select(ti => ti.GetTask()).ToList();
            List<ScheduledTask> allNewTasks = new List<ScheduledTask>();
            var tasksToBeScheduled = _scheduledTasks.Values.Where(v => v.State is TaskState.ScheduleRequested);
            _logger.LogInformation("Creating main task with {count} tasks.", tasksToBeScheduled.Count());
            foreach (var scheduledTaskInfo in tasksToBeScheduled)
            {
                await scheduledTaskInfo.SetState(TaskState.Scheduled);
                existingTasks.Add(scheduledTaskInfo.GetTask());
                allNewTasks.Add(scheduledTaskInfo);
            }

            _restartCompletionSource = new TaskCompletionSource();
            existingTasks.Add(_restartCompletionSource.Task);

            await Task.WhenAny(existingTasks);
        }
    }

    private async Task RemovedDeletedTasks()
    {
        var tasksRequestedForDeletion = _scheduledTasks.Where(st => st.Value.State == TaskState.DeleteRequested);
        foreach (var taskRequestedForDeletion in tasksRequestedForDeletion)
        {
            _logger.LogDebug("Deleting task '{Name}'", taskRequestedForDeletion.Value.Name);
            _scheduledTasks.TryRemove(taskRequestedForDeletion);
            await taskRequestedForDeletion.Value.SetState(TaskState.Deleted);
        }
    }

    public async Task Pause(string taskName) =>
        await _scheduledTasks[taskName].Pause();

    public async Task Resume(string taskName)
        => await _scheduledTasks[taskName].Resume();

    public async Task Delete(string taskName)
        => await _scheduledTasks[taskName].Delete();

    public async Task Start(string taskName)
    {
        await _scheduledTasks[taskName].SetState(TaskState.ScheduleRequested);
        if (_restartCompletionSource.Task.Status != TaskStatus.RanToCompletion)
        {
            _restartCompletionSource.SetResult();
        }

    }

    public async Task Stop(string taskName)
    {
        //_logger.LogDebug($"Requesting task `{taskName}` to be stopped");
        await _scheduledTasks[taskName].Stop();
        // _scheduledTasks[taskName].CancellationTokenSource.Cancel();        
    }


    public IEnumerator<ScheduledTask> GetEnumerator() => _scheduledTasks.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _scheduledTasks.Values.GetEnumerator();



    /// <inheritdoc />
    public IJanitor Schedule(Action<ScheduledTaskBuilder> configureBuilder)
    {
        var taskInfoBuilder = new ScheduledTaskBuilder(_serviceProvider, _logger);
        configureBuilder(taskInfoBuilder);
        var taskInfo = taskInfoBuilder.Build();
        _scheduledTasks.AddOrUpdate(taskInfo.Name, n => taskInfo, (n, t) => taskInfo);
        if (_restartCompletionSource.Task.Status != TaskStatus.RanToCompletion)
        {
            _restartCompletionSource.SetResult();
        }
        return this;
    }

    /// <inheritdoc />
    public async Task Run(string taskName) => await _scheduledTasks[taskName].Run();
}