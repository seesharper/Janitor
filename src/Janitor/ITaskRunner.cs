namespace Janitor;

/// <summary>
/// Manages schedules tasks.
/// </summary>
public interface ITaskRunner : IEnumerable<ITaskInfo>
{
    /// <summary>
    /// Starts the scheduler and returns a task that represents awaiting all tasks. 
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to stop the scheduler.</param>
    /// <returns></returns>
    Task Start(CancellationToken cancellationToken);

    /// <summary>
    /// Pauses a task. Note that it is still scheduled but not executed.
    /// </summary>
    /// <param name="taskName">The name of the the task to pause.</param>
    Task Pause(string taskName);

    /// <summary>
    /// Resumes a paused task.
    /// </summary>
    /// <param name="taskName">The name of the task to resume from a paused state.</param>
    Task Resume(string taskName);

    /// <summary>
    /// Deletes a task from the scheduler. This will cancel the task and remove it from the scheduler.
    /// </summary>
    /// <param name="taskName">The name of the task to delete.</param>
    Task Delete(string taskName);

    /// <summary>
    /// Requests the task with given <paramref name="taskName"/> to be started.
    /// </summary>
    /// <param name="taskName">The name the task to be started.</param>
    Task Start(string taskName);

    /// <summary>
    /// Requests the task with given <paramref name="taskName"/> to be stopped.
    /// </summary>
    /// <param name="taskName">The name the task to be stopped.</param>
    Task Stop(string taskName);

    ITaskRunner Schedule(Action<TaskInfoBuilder> configureBuilder);


    // /// <summary>
    // /// Adds a new scheduled <paramref name="task"/>.    
    // /// </summary>
    // /// <param name="name">The unique name of the task to b scheduled.</param>
    // /// <param name="task">The <see cref="Task"/> representing the scheduled task.</param>
    // /// <param name="waitTime">The <see cref="ISchedule"/> that represents the time to wait before executing the task.</param>
    // /// <returns><see cref="ITaskRunner"/></returns>
    // ITaskRunner Schedule(string name, Func<CancellationToken, Task> task, ISchedule waitTime);

    // /// <summary>
    // /// Adds a new scheduled <paramref name="task"/> providing the <see cref="IServiceProvider"/>
    // /// as part of the <paramref name="task"/> function.
    // /// </summary>
    // /// <param name="name">The unique name of the task to b scheduled.</param>
    // /// <param name="task">The <see cref="Task"/> representing the scheduled task.</param>
    // /// <param name="waitTime">The <see cref="ISchedule"/> that represents the time to wait before executing the task.</param>
    // /// <returns><see cref="ITaskRunner"/></returns>
    // ITaskRunner Schedule<TDependency>(string name, Func<TDependency, CancellationToken, Task> task, ISchedule waitTime);
}