using System;

namespace Siel
{
    public abstract class OneTimeTask : SielTask
    {
        protected OneTimeTask(DateTime triggerAt)
        {
            if (triggerAt <= DateTime.UtcNow)
            {
                throw new ArgumentException("Should later then now");
            }
        }

        public DateTime TriggerAt { get; private set; }

        public override long GetNextTimeSpan()
        {
            return (long) TriggerAt.Subtract(DateTime.UtcNow).TotalMilliseconds;
        }

        public override void Load(SielTask origin)
        {
            if (origin is OneTimeTask task)
            {
                TriggerAt = task.TriggerAt;
            }
        }
    }
}