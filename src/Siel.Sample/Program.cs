using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Siel.Common;
using Siel.DependencyInjection;
using Siel.MySql.DependencyInjection;
using Siel.MySql.Store;
using Siel.Scheduler;

namespace Siel.Sample
{
    public class TestTask : CronTask
    {
        private readonly ILogger _logger;
        public static int TriggerCount;

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

            Interlocked.Increment(ref TriggerCount);
            return new ValueTask<bool>(true);
        }
    }

    public class SeedData : BackgroundService
    {
        private readonly IScheduler _scheduler;

        public SeedData(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var id = CombGuid.NewGuid().ToString();
            await _scheduler.NewAsync(id, "test", new TestTask {Cron = "*/1 * * * * *"});
            await Task.Delay(12000, default);
            await _scheduler.TriggerAsync(id);
            await Task.Delay(3000, default);
            await _scheduler.UpdateAsync(id, "test", new TestTask {Cron = "*/7 * * * * *"});
            await Task.Delay(16000, default);
            await _scheduler.RemoveAsync(id);
            await _scheduler.StopAsync(default);
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            await Test();
            // await TestDependenceInjection();
            // await TestPerform();
        }

        private static async Task TestPerform()
        {
            var store = new MySqlStore(
                "Database='siel';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");

            IScheduler scheduler = new DefaultScheduler(new SerializerTaskFactory(), store, store);
            await scheduler.StartAsync(default);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int count = 100000;
            Parallel.For(0, count, new ParallelOptions
            {
                MaxDegreeOfParallelism = 50
            }, i =>
            {
                var id = CombGuid.NewGuid().ToString();
                scheduler.NewAsync(id, $"test {i}", new TestTask {Cron = "5 * * * * *"}).GetAwaiter().GetResult();
            });

            stopwatch.Stop();
            Console.WriteLine($"Create rate {count / stopwatch.ElapsedMilliseconds / 1000} jobs/s");
            Console.WriteLine($"Perform rate {TestTask.TriggerCount / stopwatch.ElapsedMilliseconds / 1000} jobs/s");
            await scheduler.StopAsync(default);
            Console.Read();
            Console.WriteLine("Bye");
        }

        private static async Task TestDependenceInjection()
        {
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
        }

        private static async Task Test()
        {
            var store = new MySqlStore(
                "Database='siel';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");

            IScheduler scheduler = new DefaultScheduler(new SerializerTaskFactory(), store, store);
            await scheduler.StartAsync(default);

            var id = CombGuid.NewGuid().ToString();
            await scheduler.NewAsync(id, "test", new TestTask {Cron = "*/5 * * * * *"});
            await Task.Delay(12000, default);
            await scheduler.TriggerAsync(id);
            await Task.Delay(3000, default);
            await scheduler.UpdateAsync(id, "test", new TestTask {Cron = "*/7 * * * * *"});
            await Task.Delay(16000, default);
            await scheduler.RemoveAsync(id);
            await scheduler.StopAsync(default);

            Console.Read();
            Console.WriteLine("Bye");
        }
    }
}