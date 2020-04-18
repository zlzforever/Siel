using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HWT;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Siel.Common;
using Siel.Store;

namespace Siel.Scheduler
{
    public class DefaultScheduler : BackgroundService, IScheduler
    {
        private readonly ITaskStore _taskStore;
        private readonly HashedWheelTimer _timer;
        private bool _initialized;
        private readonly ConcurrentDictionary<string, ITask> _dict;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITaskFactory _taskFactory;
        private readonly IEventStore _eventStore;

        public DefaultScheduler(ITaskFactory taskFactory, ITaskStore taskStore, IEventStore eventStore,
            ILoggerFactory loggerFactory = null)
        {
            _taskStore = taskStore;
            _eventStore = eventStore;
            _timer = new HashedWheelTimer();
            _dict = new ConcurrentDictionary<string, ITask>();
            _taskFactory = taskFactory;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory == null ? NullLogger.Instance : _loggerFactory.CreateLogger(GetType());
        }

        public async ValueTask<string> NewAsync(string name, SielTask task,
            Dictionary<string, string> properties = null)
        {
            var id = CombGuid.NewGuid().ToString();
            return await NewAsync(id, name, task, properties);
        }

        public async ValueTask<string> NewAsync(string id, string name, SielTask task,
            Dictionary<string, string> properties = null)
        {
            CheckIfInitialized();

            if (_taskFactory is SerializerTaskFactory)
            {
                var constructors = task.GetType().GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                if (constructors.Any(x => x.GetParameters().Length > 0))
                {
                    _logger.LogWarning(
                        "Your task type contains parameters constructor, SerializerTaskFactory can't use those constructor, maybe you want to use DependenceInjectionTaskFactory");
                }
            }

            var persistedTask = new PersistedTask(id, name, task, properties);
            if (await _taskStore.SaveAsync(persistedTask))
            {
                return EnqueueTask(persistedTask);
            }
            else
            {
                _logger.LogWarning(
                    $"Store task {persistedTask.Id}, {persistedTask.Name} failed, please make sure the id isn't duplicate");
                return null;
            }
        }

        public async ValueTask<bool> RemoveAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id should not null/empty");
            }

            if (_dict.TryGetValue(id, out var task))
            {
                task.Remove();
                return await _taskStore.RemoveAsync(id);
            }
            else
            {
                throw new ApplicationException("Remove task failed");
            }
        }

        public async ValueTask<bool> TriggerAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id should not null/empty");
            }

            if (_dict.TryGetValue(id, out var task))
            {
                await task.RunAsync(null);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            _logger.LogInformation("Scheduler stopped");

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var page = 1;
            var batch = 100;
            var count = batch;
            while (true)
            {
                var persistedTasks = await _taskStore.TakeAsync(page, batch);

                foreach (var persistedTask in persistedTasks)
                {
                    count--;
                    EnqueueTask(persistedTask);
                }

                if (count == 0)
                {
                    count = batch;
                }
                else
                {
                    break;
                }

                page++;
            }

            _initialized = true;
        }

        private string EnqueueTask(PersistedTask persistedTask)
        {
            var task = _taskFactory.Create(persistedTask.TypeName, persistedTask.Data);
            if (task == null)
            {
                throw new ApplicationException($"Create {persistedTask.Id}, {persistedTask.Name} task object failed");
            }

            task.Verify();
            task.UseLoggerFactory(_loggerFactory);
            task.Initialize(persistedTask.Id, persistedTask.Name, persistedTask.GetProperties());
            task.OnSuccess += async @event =>
            {
                _logger.LogInformation($"Execute task {@event.Id} success");
                await _eventStore.SaveSuccessAsync(@event);
            };
            task.OnFail += async @event =>
            {
                _logger.LogError($"Execute task {@event.Id} failed: {@event.StackTrace}");
                await _eventStore.SaveFailureAsync(@event);
            };

            var next = task.GetNextTimeSpan();
            _timer.NewTimeout(task, TimeSpan.FromMilliseconds(next));

            if (_dict.TryAdd(persistedTask.Id, task))
            {
                return persistedTask.Id;
            }
            else
            {
                throw new ApplicationException(
                    $"Enqueue {persistedTask.Id}, {persistedTask.Name} task object to cache failed");
            }
        }

        private void CheckIfInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Scheduler is not initialized");
            }
        }

        public async Task WaitForInitialized()
        {
            while (!_initialized)
            {
                await Task.Delay(100, default);
            }
        }
    }
}