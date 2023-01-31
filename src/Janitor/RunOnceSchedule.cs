namespace Janitor;

public class RunOnceSchedule : ISchedule
{
    private readonly DateTime _runAtUtc;

    public RunOnceSchedule(DateTime runAtUtc)
    {
        _runAtUtc = runAtUtc;
    }

    public DateTime? GetNext(DateTime utcNow)
    {
        if (_runAtUtc < utcNow)
        {
            return null;
        }
        else
        {
            return _runAtUtc;
        }
    }
}