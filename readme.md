# Janitor

The Janitor 



## What is it? 

Janitor is a simple background task runner that allows tasks to be scheduled for execution.

## Why 

While established framework exists such as Quartz and Hangfire, sometimes we need something simple that
don't bring a along to much baggage. Janitor has no database persistence or advanced features. It is just a task scheduler. No fuzz , nothing advanced. 

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
_taskRunner.Schedule(config =>
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
_taskRunner.Schedule(config =>
	config
        .WithName(TestTaskName)
        .WithSchedule(new CronSchedule("0 14 * * 1"))
        .WithScheduledTask(async (CancellationToken ct) => await SendEMailToValuedCustomers()));  
```

> We used [crontab guru](https://crontab.guru/) to generate the cron expression. 



## Dependency Injection 

Say something about injecting dependencies into handlers

## Handling state change

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








> 



