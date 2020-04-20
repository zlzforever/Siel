using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HWT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Siel.Scheduler.Event;

namespace Siel
{
    public abstract class TaskBase : ITask
    {
        private bool _removed;

        /// <summary>
        /// 任务标识
        /// </summary>
        [JsonIgnore]
        public string Id { get; private set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        [JsonIgnore]
        public string Name { get; private set; }

        /// <summary>
        /// 任务数据
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// 日志接口
        /// </summary>
        [JsonIgnore]
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// 任务执行成功通知
        /// </summary>
        internal event AsyncEventHandler<SuccessEvent> OnSuccess;

        /// <summary>
        /// 任务执行失败通知
        /// </summary>
        internal event AsyncEventHandler<FailureEvent> OnFail;

        protected abstract ValueTask<bool> HandleAsync();

        /// <summary>
        /// Get next time span
        /// </summary>
        /// <returns></returns>
        public abstract long GetNextTimeSpan();

        public void Remove()
        {
            _removed = true;
        }

        public void Initialize(string id, string name, IReadOnlyDictionary<string, string> properties)
        {
            Id = id;
            Name = name;
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    Properties.Add(property.Key, property.Value);
                }
            }
        }

        public void UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory == null ? NullLogger.Instance : loggerFactory.CreateLogger(GetType());
        }

        public async Task RunAsync(ITimeout timeout)
        {
            if (!_removed)
            {
                var success = false;
                try
                {
                    success = await HandleAsync();
                    if (OnSuccess != null)
                    {
                        // await OnSuccess.Invoke(new SuccessEvent(Id));
                    }
                }
                catch (Exception e)
                {
                    if (OnFail != null)
                    {
                        // await OnFail.Invoke(new FailureEvent(Id, e.StackTrace));
                    }
                }
                finally
                {
                    Complete(success, timeout);
                }
            }
            else
            {
                timeout.Cancel();
            }
        }

        protected virtual void Complete(bool success, ITimeout timeout)
        {
        }

        public abstract void Load(TaskBase origin);

        public abstract void Verify();
    }
}