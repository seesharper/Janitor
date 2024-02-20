using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Janitor.Tests;

public class SchedulerTests : IDisposable
{
    private CancellationTokenSource _cancellationTokenSource;
    private IJanitor _taskRunner;
    private ConcurrentDictionary<string, bool> _invocationMap = new ConcurrentDictionary<string, bool>();
    private readonly Task _testRunnerTask;

    private const string TestTaskName = "TestTask";

    private List<string> _logMessages = new List<string>();

    public SchedulerTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<SampleDependency>();
        serviceCollection.AddLogging(lb =>
        {
            lb.AddConsole().SetMinimumLevel(LogLevel.Debug);
            lb.AddProvider(new TestLoggerProvider(_logMessages));
        });
        serviceCollection.AddJanitor((sp, config) => { });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _cancellationTokenSource = new CancellationTokenSource();
        _taskRunner = serviceProvider.GetRequiredService<IJanitor>();
        _testRunnerTask = Task.Run(async () => await _taskRunner.Start(_cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }

    [Fact]
    public async Task ShouldInvokeScheduledTask()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        await WaitAWhile();
        VerifyInvoked(TestTaskName);
    }

    [Fact]
    public async Task ShouldInvokeStateHandler()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName))
                .WithStateHandler(async (SampleDependency sampleDependency) => SetInvocation(sampleDependency.GetType().Name));

        });
        await WaitAWhile();
        await _taskRunner.Pause(TestTaskName);
        await WaitAWhile();

        VerifyInvoked(typeof(SampleDependency).Name);
    }

    [Fact]
    public async Task ShouldPauseAndResumeTask()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        await WaitAWhile();
        VerifyInvoked(TestTaskName);
        await _taskRunner.Pause(TestTaskName);
        var task = _taskRunner.Single().State.Should().Be(TaskState.Paused);
        ResetInvocation(TestTaskName);
        await WaitAWhile();
        VerifyNotInvoked(TestTaskName);
        await _taskRunner.Resume(taskName: TestTaskName);
        await WaitAWhile();
        VerifyInvoked(TestTaskName);
    }

    [Fact]
    public async Task ShouldHandleAndLogFailingTask()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
       {
           config
               .WithName(TestTaskName)
               .WithSchedule(new TestSchedule())
               .WithStateHandler(async (TaskState taskState, string name, Exception exception) =>
               {
                   if (taskState == TaskState.Failed)
                   {
                       exception.Message.Should().Be("Failed");
                       name.Should().Be(TestTaskName);
                       SetInvocation("StateHandler");
                   }
               })
               .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) =>
               {
                   throw new Exception("Failed");
               });
       });
        await WaitAWhile();
        VerifyInvoked("StateHandler");
        _logMessages.Should().Contain(m => m == "Failed to execute background task 'TestTask'");
    }

    [Fact]
    public async Task ShouldStopAndStartTask()
    {
        ConcurrentQueue<TaskState> states = new ConcurrentQueue<TaskState>(new[] { TaskState.ScheduleRequested, TaskState.Scheduled, TaskState.Running, TaskState.StopRequested, TaskState.Stopped, TaskState.ScheduleRequested, TaskState.Scheduled, TaskState.Running });

        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new TestSchedule())
                .WithStateHandler(async (TaskState taskState) =>
                {
                    var nextState = states.TryDequeue(out var state) ? state : TaskState.Stopped;
                    taskState.Should().Be(nextState);
                })
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        // await WaitAWhile();
        await _taskRunner.Stop(taskName: TestTaskName);
        // _taskRunner.Single().State.Should().Be(TaskState.StopRequested);
        // await Task.Delay(millisecondsDelay: 200);
        //_taskRunner.Single().State.Should().Be(TaskState.Stopped);
        await _taskRunner.Start(taskName: TestTaskName);
        // _taskRunner.Single().State.Should().Be(TaskState.ScheduleRequested);
        // await Task.Delay(millisecondsDelay: 200);
        // _taskRunner.Single().State.Should().Be(TaskState.Scheduled);
        // ResetInvocation(TestTaskName);
        await WaitAWhile();
        await WaitAWhile();
        // VerifyInvoked(TestTaskName);
        states.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldHandleAndLogFailingStateHandler()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new TestSchedule())
                .WithStateHandler(async () =>
                {
                    throw new Exception("");
                })
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        await WaitAWhile();
        _logMessages.Should().Contain(m => m == "Failed to invoke state handler for task 'TestTask'");
    }

    [Fact]
    public async Task ShouldPreserveExistingTasksWhenAddingNewTask()
    {
        await WaitAWhile();

        _taskRunner.Schedule(config =>
       {
           config
               .WithName("1")
               .WithSchedule(new TestSchedule())
               .WithScheduledTask(async (CancellationToken ct) => SetInvocation("1"));
       });

        await WaitAWhile();

        _taskRunner.Schedule(config =>
        {
            config
                .WithName("2")
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (CancellationToken ct) => SetInvocation("2"));
        });

        await WaitAWhile();
        _taskRunner.Should().AllSatisfy(st => st.State.Should().BeOneOf(TaskState.Running, TaskState.Scheduled));

        await _taskRunner.Pause("1");

        await WaitAWhile();

        _taskRunner.Schedule(config =>
       {
           config
               .WithName("3")
               .WithSchedule(new TestSchedule())
               .WithScheduledTask(async (CancellationToken ct) => SetInvocation("3"));
       });

        _taskRunner.Single(st => st.Name == "1").State.Should().Be(TaskState.Paused);


    }



    [Fact]
    public async Task ShouldInvokeScheduledTaskOnlyOnce()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
                config
                .WithName(TestTaskName)
                .WithSchedule(new RunOnceSchedule(DateTime.UtcNow.AddMilliseconds(50)))
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName)));
        await WaitAWhile();
        VerifyInvoked(TestTaskName);
    }

    [Fact]
    public async Task ShouldNotInvokeScheduledTaskOnlyOnceWithPassedDate()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new RunOnceSchedule(DateTime.UtcNow.AddMilliseconds(-50)))
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        await WaitAWhile();
        VerifyNotInvoked(TestTaskName);
    }

    [Fact]
    public async Task ShouldStopMainTaskWhenCancelled()
    {
        await WaitAWhile();
        _cancellationTokenSource.Cancel();
        await Task.Delay(1000);
    }

    [Fact]
    public async Task ShouldDeleteTask()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        await WaitAWhile();
        await _taskRunner.Delete(TestTaskName);
        await WaitAWhile();
        _taskRunner.Should().NotContain(st => st.Name == TestTaskName);
    }

    [Fact]
    public async Task ShouldScheduleTaskWithLongDelay()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new RunOnceSchedule(DateTime.UtcNow.AddYears(1)))
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });

        await WaitAWhile();

        await _taskRunner.Delete(TestTaskName);

        await WaitAWhile();

        _taskRunner.Should().BeEmpty();
    }


    [Fact]
    public async Task ShouldEnumerateScheduledTasks()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        ((IEnumerable)_taskRunner).GetEnumerator().Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldRunTask()
    {
        await WaitAWhile();
        _taskRunner.Schedule(config =>
        {
            config
                .WithName(TestTaskName)
                .WithSchedule(new RunOnceSchedule(DateTime.UtcNow.AddYears(1)))
                .WithScheduledTask(async (SampleDependency sampleDependency, CancellationToken ct) => SetInvocation(TestTaskName));
        });
        await _taskRunner.Run(TestTaskName);

        VerifyInvoked(TestTaskName);
    }

    [Fact]
    public async Task ShouldHandleAddingMultipleTasks()
    {
        _taskRunner
        .Schedule(builder =>
        {
            builder
                .WithName("First")
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async () => SetInvocation("First"));

        })
        .Schedule(builder =>
        {
            builder
                .WithName("Second")
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async () => SetInvocation("Second"));

        });
    }


    private void SetInvocation(string name) => _invocationMap[name] = true;

    private void ResetInvocation(string name) => _invocationMap.Remove(name, out var _);

    private void VerifyInvoked(string name)
    {
        _invocationMap.ContainsKey(name).Should().BeTrue();
        _invocationMap[name].Should().BeTrue();
    }

    private void VerifyNotInvoked(string name) => _invocationMap.Should().NotContainKey(name);


    private Task WaitAWhile()
    {
        return Task.Delay(200);
    }

}


public class TestSchedule : ISchedule
{
    public DateTime? GetNext(DateTime utcNow) => utcNow.AddMilliseconds(50);
}

public class SampleDependency
{
    public int MyProperty { get; set; }
}


