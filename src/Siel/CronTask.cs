using System;
using Cronos;
using HWT;

namespace Siel
{
    /// <summary>
    /// Cron 表达式定义的任务
    /// </summary>
    public abstract class CronTask : TaskBase
    {
        /// <summary>
        /// Cron Expression
        /// </summary>
        public string Cron { get; set; }

        public override TimeSpan GetNextTimeSpan()
        {
            return GetNextTimeSpan(Cron);
        }

        public override void Load(TaskBase origin)
        {
            if (origin is CronTask task)
            {
                Cron = task.Cron;
                Retry = task.Retry;
            }
        }

        public override void Verify()
        {
            if (string.IsNullOrWhiteSpace(Cron))
            {
                throw new ArgumentException("Cron expression should not be null/empty");
            }

            var data = Cron.Split(' ');
            if (data.Length == 6)
            {
                CronExpression.Parse(Cron, CronFormat.IncludeSeconds);
            }
            else
            {
                CronExpression.Parse(Cron);
            }
        }

        private static TimeSpan GetNextTimeSpan(string cron)
        {
            var data = cron.Split(' ');
            var expression = data.Length == 6
                ? CronExpression.Parse(cron, CronFormat.IncludeSeconds)
                : CronExpression.Parse(cron);

            var now = DateTime.UtcNow;
            var nextUtc = expression.GetNextOccurrence(now);
            if (nextUtc == null)
            {
                return TimeSpan.Zero;
            }
            else
            {
                var datetime = nextUtc.Value;
                return datetime - now;
            }
        }
    }
}