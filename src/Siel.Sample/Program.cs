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
            await _scheduler.WaitForInitialized();
            var id = "a241acb9-b42a-49ba-9421-aba1011abce7";
            await _scheduler.NewAsync(id, "test", new TestTask {Cron = "*/5 * * * * *"});
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // await Test();
            await TestDependenceInjection();
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

            var id = "a241acb9-b42a-49ba-9421-aba1011abce7";
            var scheduler = new DefaultScheduler(new SerializerTaskFactory(), store, store);
            await scheduler.StartAsync(default);
            await scheduler.WaitForInitialized();
            await scheduler.NewAsync(id, "test", new TestTask {Cron = "*/5 * * * * *"});
            Thread.Sleep(12000);
            await scheduler.TriggerAsync(id);
            Thread.Sleep(3000);
            await scheduler.RemoveAsync(id);
            await scheduler.StopAsync(default);
            Console.Read();
            Console.WriteLine("Bye");
        }
    }
}