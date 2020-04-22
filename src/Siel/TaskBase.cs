using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
        private long _performedCount;
        private static long _processingCount;

        public static long ProcessingCount => Interlocked.Read(ref _processingCount);

        protected long GetPerformedCount() => Interlocked.Read(ref _performedCount);

        public int Retry { get; set; } = 1;

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

        protected abstract Task HandleAsync();

        /// <summary>
        /// Get next time span
        /// </summary>
        /// <returns></returns>
        public abstract TimeSpan GetNextTimeSpan();

        public void Remove()
        {
            _removed = true;
        }

        public void SetData(string id, string name, IReadOnlyDictionary<string, string> properties)
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

            if (Retry < 1)
            {
                Retry = 1;
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
                // todo: maybe we should create a new timeout to scheduler retry job
                // 1. 所有任务进度都存在内存中，若是进程崩溃，则所有重试信息都丢失
                // 2. 或者新开一种 RetryTask 来解决
                for (var i = 1; i < Retry + 1; ++i)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    try
                    {
                        Interlocked.Increment(ref _processingCount);

                        await HandleAsync();

                        stopwatch.Stop();

                        Interlocked.Increment(ref _performedCount);

                        if (OnSuccess != null)
                        {
                            await OnSuccess.Invoke(new SuccessEvent(Id,
                                GetType().FullName, (int) stopwatch.ElapsedMilliseconds));
                        }

                        break;
                    }
                    catch (Exception e)
                    {
                        stopwatch.Stop();
                        if (OnFail != null)
                        {
                            await OnFail.Invoke(new FailureEvent(Id, GetType().FullName,
                                (int) stopwatch.ElapsedMilliseconds, $"Execute at {i} of {Retry}: {e.Message}",
                                e.StackTrace));
                        }

                        await Task.Delay(10000);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _processingCount);
                        Complete(timeout);
                    }
                }
            }
            else
            {
                timeout.Cancel();
            }
        }

        public abstract void Load(TaskBase origin);

        public abstract void Verify();

        private void Complete(ITimeout timeout)
        {
            // manually trigger, no need to cyclical this task
            if (timeout == null)
            {
                return;
            }

            var next = GetNextTimeSpan();
            if (next > TimeSpan.Zero)
            {
                timeout.Timer.NewTimeout(this, next);
            }
        }
    }
}