# Siel

Siel is a high performance timer framework

## Overview

Incredibly easy way to perform One-Timer task, Cyclical task inside dotnet/ASP.NET core applications. CPU and I/O intensive, long-running and short-running jobs are supported. 
Backed by MySql, SQL Server and some other RDB. It can manage million tasks by one instance

## Installation

Siel is available as a NuGet package. You can install it using the NuGet Package Console window:

PM> Install-Package Siel

## Usage

1. Use in a console application, impl a cyclical task

``` c#

    public class TestTask : CyclicalTask
    {
        private readonly ILogger _logger;

        public TestTask()
        {
        }

        public TestTask(ILogger<TestTask> logger)
        {
            _logger = logger;
        }

        protected override ValueTask<bool> HandleAsync()
        {
            var msg = $"Id {Id}, Name: {Name}, Cron: {Cron}, TriggerAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            if (_logger != null)
            {
                _logger.LogInformation(msg);
            }
            else
            {
                Console.WriteLine(msg);
            }

            return new ValueTask<bool>(true);
        }
    }

```

```c#
            var store = new MySqlStore(
                "Database='siel';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");

            var scheduler = new DefaultScheduler(new SerializerTaskFactory(), store, store);
            await scheduler.StartAsync(default);
            await scheduler.WaitForInitialized();
            await scheduler.NewAsync("test", new TestTask {Cron = "*/5 * * * * *"});
            Thread.Sleep(12000);
            await scheduler.TriggerAsync(id);
            Thread.Sleep(3000);
            await scheduler.RemoveAsync(id);
            await scheduler.StopAsync(default);
            Console.Read();
            Console.WriteLine("Bye");
```

2. Use in DependencyInjection

Impl a seed data background service

```c#
    public class SeedData : BackgroundService
    {
        private readonly IScheduler _scheduler;

        public SeedData(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _scheduler.WaitForInitialized();
            var id = "a241acb9-b42a-49ba-9421-aba1011abce7";
            await _scheduler.NewAsync(id, "test", new TestTask {Cron = "*/5 * * * * *"});
        }
    }
```

Impl a host service
```c#

 await Host.CreateDefaultBuilder().ConfigureServices(x =>
            {
                x.AddScoped<TestTask>();
                x.AddSiel(builder =>
                {
                    builder.UseDependenceInjectionTaskFactory();
                    builder.UseMySqlStore(
                        "Database='siel';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");
                });
                x.AddHostedService<SeedData>();
            }).Build().RunAsync();
```

