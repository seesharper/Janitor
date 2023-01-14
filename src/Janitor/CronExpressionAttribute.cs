namespace Janitor;

/// <summary>
/// Specifies the "schedule" for a background task.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CronExpressionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpressionAttribute"/> class.
    /// </summary>
    /// <param name="expression">The cron expression representing the scheduling of the background task.</param>
    public CronExpressionAttribute(string expression) => Expression = expression;

    /// <summary>
    /// Gets the cron expression representing the scheduling of the background task.
    /// </summary>
    public string Expression { get; }
}