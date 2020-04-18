using System;
using Cronos;
using HWT;

namespace Siel
{
    public abstract class CyclicalTask : SielTask
    {
        private int _retried;

        /// <summary>
        /// Cron Expression
        /// </summary>
        public string Cron { get; set; }

        public override long GetNextTimeSpan()
        {
            return (long) GetNextTimestamp(Cron).TotalMilliseconds;
        }

        public override void Load(SielTask origin)
        {
            if (origin is CyclicalTask task)
            {
                Cron = task.Cron;
            }
        }

        public override void Verify()
        {
            if (string.IsNullOrWhiteSpace(Cron))
            {
                throw new ArgumentException("Cron expression should not be null/empty");
            }

            var next = GetNextTimeSpan();
            if (next == 0)
            {
                throw new ArgumentException($"Cron expression {Cron} isn't valid");
            }
        }

        protected override void Complete(bool success, ITimeout timeout)
        {
            // manually trigger, no need to cyclical this task
            if (timeout == null)
            {
                return;
            }

            if (!success)
            {
                _retried++;
            }
            else
            {
                _retried = 0;
            }

            if (_retried > 3)
            {
                return;
            }

            var next = GetNextTimeSpan();
            if (next > 0)
            {
                timeout.Timer.NewTimeout(this, TimeSpan.FromMilliseconds(next));
            }
        }

        private static TimeSpan GetNextTimestamp(string cron)
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