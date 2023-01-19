using Cronos;

namespace Janitor;

/// <summary>
/// An <see cref="ISchedule"/> that uses a cron expression
/// to calculate the time to wait before the next execution of a task.
/// </summary>
public class CronScheduler : ISchedule
{
    private readonly CronExpression _cronExpression;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronScheduler"/> class.
    /// </summary>
    /// <param name="cronExpression">The cron expression to be used to 
    /// calculate the time to wait before the next execution of a task.</param>
    public CronScheduler(string cronExpression)
        => _cronExpression = CronExpression.Parse(cronExpression);

    /// <inheritdoc/>    
    public DateTime? GetNext(DateTime utcNow)
        => _cronExpression.GetNextOccurrence(utcNow);
}

public static class Schedule
{
    public static ISchedule FromCronExpression(string cronExpression) => new CronScheduler(cronExpression);
}