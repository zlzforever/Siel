using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Siel.DependencyInjection;
using Siel.MySql.DependencyInjection;
using Siel.MySql.Store;
using Siel.Scheduler;

namespace Siel.Sample
{
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

    public class SeedData : BackgroundService
    {
        private readonly IScheduler _scheduler;

        public SeedData(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var id = "a241acb9-b42a-49ba-9421-aba1011abce7";
            await _scheduler.NewAsync(id, "test", new TestTask {Cron = "*/5 * * * * *"});
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

            var id = "a241acb9-b42a-49ba-9421-aba1011abce7";
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