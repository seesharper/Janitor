# Janitor

## What is it? 

Janitor is a simple background task runner that allows tasks to be scheduled for execution.

## Why 

One example is hosted services in Asp.Net apps. 

## Example 

```csharp
serviceCollection.AddJanitor((sp, config) =>
{
    config.Schedule("MyTask", async (cancellationToken) =>
        Console.Out.WriteLineAsync("Hello from MyTask", cancellationToken)
    , Schedule.At(DateTime.UtcNow.AddHours(1)));         
});
```

All tasks are executed within their own container scope.


```csharp
serviceCollection.AddJanitor((sp, config) =>
{
    config.Schedule<IDbConnection>("MyTask", async (dbConnection, cancellationToken) =>
        Console.Out.WriteLineAsync("Hello from MyTask", cancellationToken)
    , Schedule.At(DateTime.UtcNow.AddHours(1)));         
});
```

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








> Note: It is very important that we pass along the `CancellationToken` since that is how we stop/cancel tasks.

Example with cron expression 

```csharp
serviceCollection.AddJanitor((sp, config) =>
{
    config.Schedule("MyTask", async (cancellationToken) =>
        Console.Out.WriteLineAsync("Hello from MyTask", cancellationToken)
    , Schedule.FromCronExpression("*/1 * * * *"));         
});
```




## Scheduling tasks 

The `IScheduler` is what we use for scheduling a task. It has one purpose and that is to return the DateTime(UTC) for 
the next execution of the task. If it returns `NULL` or a DateTime that has already passed, the task will be removed and would have to be rescheduled in order for it to be executed again. 

The following schedulers comes out of the box

* CronScheduler - Returns the next execution occurrence using a cron expression.
* RunOnceScheduler - Runs the scheduled task only once at a given DateTime(UTC) and then removes the task.
  

