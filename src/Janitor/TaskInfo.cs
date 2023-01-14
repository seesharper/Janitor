using Cronos;

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

/// <summary>
/// Provides a <see cref="TimeSpan"/> that represents the time wo wait until 
/// the next execution of a task.
/// </summary>
public interface IWaitTime
{
    /// <summary>
    /// Gets the <see cref="TimeSpan"/> that represents the time wo wait until 
    /// the next execution of a task.
    /// </summary>
    /// <returns>The <see cref="TimeSpan"/> that represents the time wo wait until 
    /// the next execution of a task.</returns>
    TimeSpan? GetWaitTime();
}

/// <summary>
/// An <see cref="IWaitTime"/> that uses a cron expression
/// to calculate the time to wait before the next execution of a task.
/// </summary>
public class CronWaitTime : IWaitTime
{
    private readonly CronExpression _cronExpression;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronWaitTime"/> class.
    /// </summary>
    /// <param name="cronExpression">The cron expression to be used to 
    /// calculate the time to wait before the next execution of a task.</param>
    public CronWaitTime(string cronExpression)
        => _cronExpression = CronExpression.Parse(cronExpression);

    /// <inheritdoc/>    
    public TimeSpan? GetWaitTime()
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime? nextOccurrence = _cronExpression.GetNextOccurrence(utcNow);
        return nextOccurrence - utcNow;
    }
}
