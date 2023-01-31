namespace Janitor;

/// <summary>
/// Describes the state of a given task.
/// </summary>
public enum TaskState
{
    /// <summary>
    /// The task is requested to be started
    /// </summary>
    ScheduleRequested,

    /// <summary>
    /// The task is started.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The task is running/executing.
    /// </summary>
    Running,

    /// <summary>
    /// The task is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// The task is in the process of being stopped.
    /// </summary>
    StopRequested,

    /// <summary>
    /// The task has been stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// The task is requested to be deleted
    /// </summary>
    DeleteRequested,

    /// <summary>
    /// The task was deleted
    /// </summary>
    Deleted,

    /// <summary>
    /// The task has failed to execute.
    /// </summary>
    Failed
}