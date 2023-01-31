namespace Janitor;

/// <summary>
/// Provides a <see cref="TimeSpan"/> that represents the time wo wait until 
/// the next execution of a task.
/// </summary>
public interface ISchedule
{
    /// <summary>
    /// Gets the next <see cref="DateTime"/> (UTC) for which a given task will be executed.
    /// </summary>
    /// <returns>The the next <see cref="DateTime"/> (UTC) for which a given task will be executed.</returns>
    DateTime? GetNext(DateTime utcNow);
}
