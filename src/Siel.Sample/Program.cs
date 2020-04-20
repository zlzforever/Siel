using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Siel.Common;
using Siel.DependencyInjection;
using Siel.MySql.DependencyInjection;
using Siel.MySql.Store;
using Siel.Scheduler;
using Siel.Store;

namespace Siel.Sample
{
    public class TestTask : CronTask
    {
        protected override Task HandleAsync()
        {
            var msg =
                $"Id {Id}, Name: {Name}, Cron: {Cron}, Properties: {JsonConvert.SerializeObject(Properties)} TriggerAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            Logger.LogInformation(msg);
            return Task.CompletedTask;
        }
    }

    public class PerformTask : CronTask
    {
        public static long TriggerCount;

        protected override Task HandleAsync()
        {
            Interlocked.Increment(ref TriggerCount);
            return Task.CompletedTask;
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
            var store = new InMemoryStore();

            IScheduler scheduler = new DefaultScheduler(new SerializerTaskFactory(), store, store);
            await scheduler.StartAsync(default);

            int count = 500000;

            for (int i = 0; i < count; ++i)
            {
                var id = CombGuid.NewGuid().ToString();
                await scheduler.NewAsync(id, $"test {i}", new PerformTask {Cron = "*/1 * * * * ?"});
            }

            var c1 = PerformTask.TriggerCount;
            await Task.Delay(11000, default);
            var c2 = PerformTask.TriggerCount;
            var c = c2 - c1;
            Console.WriteLine($"Perform {c / 11} jobs/s");
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
                    builder.UseDependencyInjectionTaskFactory();
                    builder.UseMySql(
                        "Database='siel';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");
                });
                x.AddHostedService<SeedData>();
            }).Build().RunAsync();
        }

        private static async Task Test()
        {
            var store = new MySqlStore(
                "Database='siel';Data Source=localhost;password=1qazZAQ!;User ID=root;Port=3306;");

            var loggerFactory = LoggerFactory.Create(x => { x.AddConsole(); });
            IScheduler scheduler = new DefaultScheduler(new SerializerTaskFactory(), store, store, loggerFactory);
            await scheduler.StartAsync(default);

            var id1 = CombGuid.NewGuid().ToString();
            await scheduler.NewAsync(id1, "test", new TestTask {Cron = "*/3 * * * * *"});
            await Task.Delay(12000, default);
            await scheduler.TriggerAsync(id1);
            await Task.Delay(3000, default);
            await scheduler.UpdateAsync(id1, "test", new TestTask {Cron = "*/7 * * * * *"});
            await Task.Delay(16000, default);
            var queryResult = await scheduler.PagedQueryAsync(null, 0, 100);
            Console.WriteLine(
                $"Page {queryResult.Page}, Count {queryResult.Count}, Limit {queryResult.Limit}, Data: {JsonConvert.SerializeObject(queryResult.Data)}");
            await scheduler.RemoveAsync(id1);
            await scheduler.StopAsync(default);

            Console.Read();
            Console.WriteLine("Bye");
        }
    }
}