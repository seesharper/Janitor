# Janitor

The Janitor 



## What is it? 

Janitor is a simple background task runner that allows tasks to be scheduled for execution.

## Why 

While established framework exists such as Quartz and Hangfire, sometimes we need something simple that
don't bring a along to much baggage. Janitor has no database persistence or advanced features. It is just a task scheduler. No fuzz , nothing advanced. 

## Getting started 

Say that we have some arbitrary code like this 

```csharp
public async Task SendEMailToValuedCustomers(CancellationToken ct)
{
    // Code that send Emails here ...... 
}  
```

Say that we would want to execute `SendEMailToValuedCustomers` 

The following code will schedule the method to execute in 60 seconds.

```csharp
_janitor.Schedule(config =>
	config
        .WithName(TestTaskName)
        .WithSchedule(new RunOnceSchedule(DateTime.UtcNow.AddSeconds(60)))
        .WithScheduledTask(async (CancellationToken ct) => await SendEMailToValuedCustomers()));          
```

## Schedule 

The Janitor uses an `ISchedule` to determine the date and time for the next execution of the task.

```csharp
public interface ISchedule
{    
    DateTime? GetNext(DateTime utcNow);
}
```

The purpose of `ISchedule` is to provide the `DateTime` for the next execution based upon the current `DateTime(UTC)`
If the `GetNext` method return `null` or a point on time that has already passed the scheduled task will be removed from the Janitor.
As long as we provide a `DateTime` representing some time in the future, the Janitor will schedule it for execution.

This means that we can implement an `ISchedule` that continuously just keeps giving back a new date based on for instance a cron expression.

```csharp
public class CronSchedule : ISchedule
{
    private readonly CronExpression _cronExpression;
    
    public CronSchedule(string cronExpression)
        => _cronExpression = CronExpression.Parse(cronExpression);
 
    public DateTime? GetNext(DateTime utcNow)
        => _cronExpression.GetNextOccurrence(utcNow);
}
```

> Note: The code above uses the [Cronos](https://github.com/HangfireIO/Cronos) library 

So say that we wanted to execute `SendEMailToValuedCustomers` every monday at 2PM. 

```csharp
_janitor.Schedule(config =>
	config
        .WithName(TestTaskName)
        .WithSchedule(new CronSchedule("0 14 * * 1"))
        .WithScheduledTask(async (CancellationToken ct) => await SendEMailToValuedCustomers(ct)));  
```

> We used [crontab guru](https://crontab.guru/) to generate the cron expression. 

## Task Management

Scheduled tasks can be added, stopped, started, paused and resumed at any time. 

* Schedule - Schedules a new task with the Janitor.
* Stop - Stops a scheduled task 
* Start- Starts a scheduled task if it has been stopped
* Pause - Pauses the task without stopping it. 
* Resumes - Resumes a paused task
* Delete - Deletes a task from the Janitor. 
* Run - Executes the task immediately without waiting for the given schedule. Useful for testing. 

 Note that it is VERY IMPORTANT that scheduled tasks passes the `CancellationToken` down to other tasks that gets awaited inside a scheduled task. The Janitor uses the token to cancel the task when it is requested for stopping or deletion. 

## Dependency Injection 

Janitor follows much of the same pattern as AspNet minimal API's where we can inject dependencies directly into our handlers. For instance if we wanted to inject an `IDbConnection`into a scheduled task we can do this simply by providing the parameter to the delegate that represents our scheduled task. The services to be injected must be registered in `IServiceCollection` before invoking the task.

```c#
_janitor.Schedule(config =>
	config
        .WithName(TestTaskName)
        .WithSchedule(new CronSchedule("0 14 * * 1"))
        .WithScheduledTask(async (IDbConnection dbConnection, CancellationToken ct) => await SendEMailToValuedCustomers(ct))); 
```

> Note: Janitor will ALWAYS create a new container-scope before invoking the scheduled task.

In addition to injecting registered services into a scheduled task there are some types that will always be available.

* CancellationToken - Used to cancel scheduled tasks 

* IServiceProvider - The root `IServiceProvider`

* IServiceScope - The container scope that is created when executing the scheduled task

  

 The same goes for state handlers. 

```c#
_janitor.Schedule(config =>
	config
        .WithName(TestTaskName)
        .WithSchedule(new CronSchedule("0 14 * * 1"))
        .WithStateHandler(async (string name, TaskState state, IDbConnection dbConnection) => {
        	    
        })          
        .WithScheduledTask(async (IDbConnection dbConnection, CancellationToken ct) => await SendEMailToValuedCustomers())); 
```



## Handling state change

State handlers are used to keep track of the current state for a given scheduled task. This can be useful for applications that monitor the state of scheduled tasks and we can use these handlers to react to state changes. 

```csharp
serviceCollection.AddJanitor((sp, config) =>
{
    config.Schedule("MyTask", async (cancellationToken) =>
        Console.Out.WriteLineAsync("Hello from MyTask", cancellationToken)
    , Schedule.At(DateTime.UtcNow.AddHours(1)))
    .WithStateHandler(async (taskInfo, cancellationToken) => Console.WriteLine(taskInfo.State));  
});
```
State handlers with dependencies

```csharp
serviceCollection.AddJanitor((sp, config) =>
{
    config.Schedule("MyTask", async (cancellationToken) =>
        Console.Out.WriteLineAsync("Hello from MyTask", cancellationToken)
    , Schedule.At(DateTime.UtcNow.AddHours(1)))
    .WithStateHandler<IDbConnection>(async (taskInfo, dbConnection, cancellationToken) => Console.WriteLine(taskInfo.State));  
});
```

## Hosting

Janitor has no dependencies to any specific host meaning that it can be used in any kind of applications. The following example is just a simple console app with a generic host. Note that the generic host has no dependencies to AspNet although we would have the same pattern if this was an AspNet application. The following example shows how to create a generic host and schedule a task to be executed every 5 seconds. 

```csharp
var host = Host.CreateDefaultBuilder(Args.ToArray())
    .ConfigureServices(services =>
    {
        services.AddJanitor((sp, config) =>
        {
            config.Schedule(builder =>
            {
                builder
                    .WithName("MyTask")
                    .WithSchedule(new TimeSpanSchedule(TimeSpan.FromSeconds(5)))
                    .WithScheduledTask(async (CancellationToken ct) =>
                    {
                        await Task.Delay(100, ct);
                        Console.WriteLine("Doing work");
                    });
            });
        })
        .AddHostedService<JanitorBackgroundService>();
    }).Build();
    
await host.RunAsync();
```

The `JanitorBackgroundService` is just an implementation of [BackgroundService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice) 

```csharp
public class JanitorBackgroundService : BackgroundService
{
    private readonly IJanitor _janitor;

    public JanitorBackgroundService(IJanitor janitor)
        => _janitor = janitor;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        => await _janitor.Start(stoppingToken);
}
```










> 



