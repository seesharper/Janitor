using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Janitor.Tests;

public class SchedulerTests : IDisposable
{
    private CancellationTokenSource _cancellationTokenSource;
    private ITaskRunner _taskRunner;

    private ConcurrentDictionary<string, bool> _invocationMap = new ConcurrentDictionary<string, bool>();

    private Task _testRunnerTask;


    public SchedulerTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<SampleDependency>();
        serviceCollection.AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Debug));
        serviceCollection.AddJanitor((sp, config) => { });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _cancellationTokenSource = new CancellationTokenSource();
        _taskRunner = serviceProvider.GetRequiredService<ITaskRunner>();
        _testRunnerTask = Task.Run(async () => await _taskRunner.Start(_cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }

    [Fact]
    public async Task ShouldInvokeScheduledTask()
    {
        await Task.Delay(200);
        _taskRunner.Schedule<SampleDependency>(config =>
        {
            config
                .WithName("Test")
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (sampleDependency, ct) => SetInvocation("Test"));
        });
        await Task.Delay(200);
        VerifyInvoked("Test");
    }

    [Fact]
    public async Task ShouldInvokeStateHandler()
    {
        await Task.Delay(200);
        _taskRunner.Schedule<SampleDependency>(config =>
        {
            config
                .WithName("Test")
                .WithSchedule(new TestSchedule())
                .WithScheduledTask(async (sampleDependenecy, ct) => SetInvocation("Test"))
                .WithStateHandler(async (SampleDependency sampleDependency) => SetInvocation(sampleDependency.GetType().Name));

        });
        await Task.Delay(200);
        await _taskRunner.Pause("Test");
        await Task.Delay(200);

        VerifyInvoked(typeof(SampleDependency).Name);
    }


    // [Fact]
    // public async Task ShouldPauseAndResumeTask()
    // {
    //     await Task.Delay(200);
    //     _taskRunner.Schedule("TEST", async (ct) => SetInvocation("TEST"), new TestSchedule());
    //     await Task.Delay(200);
    //     VerifyInvoked("TEST");
    //     _taskRunner.Pause("TEST");
    //     var task = _taskRunner.Single().State.Should().Be(TaskState.Paused);
    //     ResetInvocation("TEST");
    //     await Task.Delay(200);
    //     VerifyNotInvoked("TEST");
    //     _taskRunner.Resume(taskName: "TEST");
    //     await Task.Delay(200);
    //     VerifyInvoked("TEST");
    // }

    // [Fact]
    // public async Task ShouldScheduleTaskWithCronExpresssion()
    // {
    //     _taskRunner.Schedule("TEST", async (ct) => SetInvocation("TEST"), Schedule.FromCronExpression("*/1 * * * *"));
    //     await Task.Delay(TimeSpan.FromSeconds(61));
    //     VerifyInvoked("TEST");
    // }

    // [Fact]
    // public async Task ShouldHandleAndLogFailingTask()
    // {
    //     await Task.Delay(200);
    //     _taskRunner.Schedule("TEST", async (ct) => throw new Exception("Failed"), new TestSchedule());
    //     await Task.Delay(200);
    // }

    // [Fact]
    // public async Task ShouldStopAndStartTask()
    // {
    //     await Task.Delay(200);
    //     _taskRunner.Schedule("TEST", async (ct) => SetInvocation("TEST"), new TestSchedule());
    //     await Task.Delay(200);
    //     _taskRunner.Stop("TEST");
    //     _taskRunner.Single().State.Should().Be(TaskState.StopRequested);
    //     await Task.Delay(200);
    //     _taskRunner.Single().State.Should().Be(TaskState.Stopped);
    //     _taskRunner.Start("TEST");
    //     _taskRunner.Single().State.Should().Be(TaskState.StartRequested);
    //     await Task.Delay(200);
    //     _taskRunner.Single().State.Should().Be(TaskState.Started);

    // }


    private void SetInvocation(string name) => _invocationMap[name] = true;

    private void ResetInvocation(string name) => _invocationMap.Remove(name, out var _);

    private void VerifyInvoked(string name)
    {
        _invocationMap.ContainsKey(name).Should().BeTrue();
        _invocationMap[name].Should().BeTrue();
    }

    private void VerifyNotInvoked(string name) => _invocationMap.Should().NotContainKey(name);

}


public class TestSchedule : ISchedule
{
    public DateTime? GetNext(DateTime utcNow) => utcNow.AddMilliseconds(100);
}

public class SampleDependency
{
    public int MyProperty { get; set; }
}


