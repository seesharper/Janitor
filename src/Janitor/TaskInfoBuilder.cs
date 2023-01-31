using Microsoft.Extensions.Logging;

namespace Janitor;

public class ScheduledTaskBuilder
{
    private string _name;
    private Delegate _taskToBeScheduled;
    private ISchedule _schedule;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IJanitor> _logger;

    private Delegate _stateHandler;

    public ScheduledTaskBuilder(IServiceProvider serviceProvider, ILogger<IJanitor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ScheduledTaskBuilder WithStateHandler(Delegate handler)
    {
        _stateHandler = handler;
        return this;
    }

    public ScheduledTaskBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ScheduledTaskBuilder WithSchedule(ISchedule schedule)
    {
        _schedule = schedule;
        return this;
    }

    public ScheduledTaskBuilder WithScheduledTask(Delegate taskHandler)
    {
        _taskToBeScheduled = taskHandler;
        return this;
    }

    public ScheduledTask Build()
    {
        if (_stateHandler is null)
        {
            _stateHandler = () => { return Task.CompletedTask; };
        }
        var taskInfo = new ScheduledTask(_name, _taskToBeScheduled, _serviceProvider, _schedule, _stateHandler, _logger);

        return taskInfo;
    }
}
