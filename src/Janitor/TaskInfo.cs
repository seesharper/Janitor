namespace Janitor;

/// <summary>
/// Represents the task to be scheduled.
/// </summary>
/// <param name="Name">The unique name of the task.</param>
/// <param name="Task">The <see cref="Task"/> to be scheduled.</param>
/// <param name="waitTime">The IWaitTime </param>
public record TaskInfo(string Name, Func<IServiceProvider, CancellationToken, Task> Task, IWaitTime waitTime)
{
    /// <summary>
    /// Gets or sets the scheduled task.
    /// </summary>
    public Task? ScheduledTask { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="CancellationTokenSource"/> that is used to cancel the task.
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// Gets or sets the current state of the task.
    /// </summary>
    public TaskState State { get; set; }
}
