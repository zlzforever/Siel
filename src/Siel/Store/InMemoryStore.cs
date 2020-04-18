using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Siel.Common;
using Siel.Scheduler.Event;

namespace Siel.Store
{
    public sealed class InMemoryStore : ITaskStore, IEventStore
    {
        private readonly ConcurrentDictionary<string, PersistedTask> _dict;
        private readonly ConcurrentQueue<FailureEvent> _failureEvents;
        private readonly ConcurrentQueue<SuccessEvent> _successEvents;

        public InMemoryStore()
        {
            _dict = new ConcurrentDictionary<string, PersistedTask>();
            _failureEvents = new ConcurrentQueue<FailureEvent>();
            _successEvents = new ConcurrentQueue<SuccessEvent>();
        }

        public ValueTask<bool> SaveAsync(PersistedTask task)
        {
            return new ValueTask<bool>(_dict.TryAdd(task.Id, task));
        }

        public Task<IEnumerable<PersistedTask>> TakeAsync(int page, int limit)
        {
            if (page <= 0)
            {
                throw new ArgumentException("Page should larger than 1");
            }

            if (limit <= 0)
            {
                throw new ArgumentException("Limit should larger than 1");
            }

            var start = (page - 1) * limit;

            return Task.FromResult(_dict.Values.Skip(start).Take(limit));
        }

        public ValueTask<bool> RemoveAsync(string id)
        {
            return new ValueTask<bool>(_dict.TryRemove(id, out _));
        }

        public Task<PagedResult<PersistedTask>> PagedQueryAsync(string keyword, int page, int limit)
        {
            var count = _dict.Count;
            if (page <= 0)
            {
                throw new ArgumentException("Page should larger than 1");
            }

            if (limit <= 0)
            {
                throw new ArgumentException("Limit should larger than 1");
            }

            var result = new PagedResult<PersistedTask> {Count = count, Page = page, Limit = limit};
            var start = (page - 1) * limit;
            result.Data = string.IsNullOrWhiteSpace(keyword)
                ? _dict.Values.Skip(start).Take(limit).ToList()
                : _dict.Values.Where(x => x.Name.Contains(keyword.Trim())).Skip(start).Take(limit).ToList();

            return Task.FromResult(result);
        }

        public Task SaveFailureAsync(FailureEvent @event)
        {
            _failureEvents.Enqueue(@event);
            if (_dict.TryGetValue(@event.Id, out var persistedTask))
            {
                persistedTask.Fail();
            }

            return Task.CompletedTask;
        }

        public Task SaveSuccessAsync(SuccessEvent @event)
        {
            _successEvents.Enqueue(@event);
            if (_dict.TryGetValue(@event.Id, out var persistedTask))
            {
                persistedTask.Success();
            }

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(_dict);
        }
    }
}