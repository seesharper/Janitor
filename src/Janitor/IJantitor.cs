namespace Janitor;

/// <summary>
/// Manages schedules tasks.
/// </summary>
public interface IJanitor : IEnumerable<ScheduledTask>
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

    /// <summary>
    /// Schedules a new task.
    /// </summary>
    /// <param name="configureBuilder"></param>
    /// <returns></returns>
    IJanitor Schedule(Action<ScheduledTaskBuilder> configureBuilder);
}