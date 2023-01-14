namespace Janitor;

/// <summary>
/// Describes the state of a given task.
/// </summary>
public enum TaskState
{

    /// <summary>
    /// Task is about to be scheduled 
    /// </summary>
    ToBeScheduled,

    /// <summary>
    /// The task is running/executing.
    /// </summary>
    Running,

    /// <summary>
    /// The task is scheduled for execution
    /// </summary>
    Scheduled,

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
    /// The process in in the process of being deleted.
    /// </summary>
    DeleteRequested
}