using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HWT;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Siel.Common;
using Siel.Store;

namespace Siel.Scheduler
{
    public class DefaultScheduler : BackgroundService, IScheduler
    {
        private readonly ITaskStore _taskStore;
        private readonly HashedWheelTimer _timer;
        private int _initialized;
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

        public async ValueTask<string> NewAsync(string name, ITask task,
            Dictionary<string, string> properties = null)
        {
            var id = CombGuid.NewGuid().ToString();
            return await NewAsync(id, name, task, properties);
        }

        public async ValueTask<string> NewAsync(string id, string name, ITask task,
            Dictionary<string, string> properties = null)
        {
            CheckIdAndName(id, name);

            if (_taskFactory is SerializerTaskFactory)
            {
                var constructors = task.GetType().GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                if (constructors.Any(x => x.GetParameters().Length > 0))
                {
                    _logger.LogWarning(
                        "Your task contains parameters constructor, SerializerTaskFactory may can't use those constructors, Do you want to try DependenceInjectionTaskFactory?");
                }
            }

            await WaitForInitialized();

            var persistedTask = new PersistedTask(id, name, task, properties);
            if (await _taskStore.SaveAsync(persistedTask))
            {
                return EnqueueTask(persistedTask);
            }
            else
            {
                _logger.LogWarning(
                    $"Save task {persistedTask.Id}: {JsonConvert.SerializeObject(task)} to storage failed");
                return null;
            }
        }

        public async ValueTask<bool> UpdateAsync(string id, string name, ITask task,
            Dictionary<string, string> properties = null)
        {
            CheckIdAndName(id, name);

            if (_dict.TryRemove(id, out var cacheTask))
            {
                cacheTask.Remove();
                var persistedTask = new PersistedTask(id, name, task, properties);
                if (await _taskStore.UpdateAsync(persistedTask))
                {
                    EnqueueTask(persistedTask);
                    return true;
                }
                else
                {
                    throw new ApplicationException(
                        $"Update task {persistedTask.Id}: {JsonConvert.SerializeObject(task)} to storage failed");
                }
            }
            else
            {
                throw new ApplicationException(
                    $"Find task {id} in cache failed");
            }
        }

        public async ValueTask<bool> RemoveAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id should be not null/empty");
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
                throw new ArgumentException("Id should be not null/empty");
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

        public async Task<PagedResult<PersistedTask>> PagedQueryAsync(string keyword, int page, int limit)
        {
            return await _taskStore.PagedQueryAsync(keyword, page, limit);
        }

        public Task<SielStatus> GetStatusAsync()
        {
            return Task.FromResult(new SielStatus
            {
                ProcessingCount = TaskBase.ProcessingCount,
                PendingTimeouts = (int) _timer.PendingTimeouts
            });
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
            var batch = 200;
            var count = batch;
            while (true)
            {
                var tasks = await _taskStore.TakeAsync(page, batch);

                foreach (var task in tasks)
                {
                    count--;
                    EnqueueTask(task);
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

            Interlocked.Exchange(ref _initialized, 1);
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private void CheckIdAndName(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (id.Length > 36)
            {
                throw new ArgumentOutOfRangeException($"The length of id should be less than 36");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length > 255)
            {
                throw new ArgumentOutOfRangeException($"The length of name should be less than 255");
            }
        }

        private async Task WaitForInitialized()
        {
            while (_initialized != 1)
            {
                await Task.Delay(100, default);
            }
        }

        private string EnqueueTask(PersistedTask persistedTask)
        {
            var task = _taskFactory.Create(persistedTask.TypeName, persistedTask.Data);
            task.Verify();
            task.UseLoggerFactory(_loggerFactory);
            task.Initialize(persistedTask.Id, persistedTask.Name, persistedTask.GetProperties());
            task.OnSuccess += async @event =>
            {
                _logger.LogInformation($"Execute task {@event.Id} success");
                await _eventStore.SuccessAsync(@event);
            };
            task.OnFail += async @event =>
            {
                _logger.LogError($"Execute task {@event.Id} failed: {@event.StackTrace}");
                await _eventStore.FailAsync(@event);
            };

            var next = task.GetNextTimeSpan();
            if (next > TimeSpan.Zero)
            {
                _timer.NewTimeout(task, next);

                if (_dict.TryAdd(persistedTask.Id, task))
                {
                    return persistedTask.Id;
                }
                else
                {
                    throw new ApplicationException(
                        $"Enqueue task  {persistedTask.Id} object to cache failed");
                }
            }
            else
            {
                throw new ApplicationException(
                    $"Enqueue task {persistedTask.Id}: {JsonConvert.SerializeObject(task)} failed because it will occur never");
            }
        }
    }
}