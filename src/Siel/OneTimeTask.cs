using System;

namespace Siel
{
    public abstract class OneTimeTask : TaskBase
    {
        public DateTime TriggerAt { get; private set; }

        public override void Verify()
        {
            if (TriggerAt <= DateTime.UtcNow)
            {
                throw new ArgumentException("Should later then now");
            }
        }

        public override TimeSpan GetNextTimeSpan()
        {
            return GetPerformedCount() > 0 ? TimeSpan.Zero : TriggerAt.Subtract(DateTime.UtcNow);
        }

        public override void Load(TaskBase origin)
        {
            if (origin is OneTimeTask task)
            {
                TriggerAt = task.TriggerAt;
                Retry = task.Retry;
            }
        }
    }
}