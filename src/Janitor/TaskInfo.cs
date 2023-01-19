namespace Janitor;

/// <summary>
/// Represents the task to be scheduled.
/// </summary>
/// <param name="Name">The unique name of the task.</param>
/// <param name="Task">The <see cref="Task"/> to be scheduled.</param>
/// <param name="scheduler">The IWaitTime </param>
// public record TaskInfo(string Name, Func<TaskInfo, CancellationToken, Task> Task, Func<TaskInfo, CancellationToken, Task> StateHandler)
public record TaskInfo(string Name, Func<TaskInfo, CancellationToken, Task> Task)
{
    /// <summary>
    /// Gets or sets the <see cref="CancellationTokenSource"/> that is used to cancel the task.
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// Gets or sets the current state of the task.
    /// </summary>
    public TaskState State { get; private set; } = TaskState.StartRequested;

    public async Task SetState(TaskState newState)
    {
        State = newState;
        // await StateHandler(this, CancellationTokenSource!.Token);
    }
}
