using System;

namespace Siel.Scheduler.Event
{
    public struct FailureEvent
    {
        public string Id { get; private set; }
        public string StackTrace { get; private set; }
        public int Duration { get; private set; }
        public string Type { get; private set; }
        public string Message { get; private set; }

        public FailureEvent(string id, string type,
            int duration, string message, string stackTrace)
        {
            Id = id;
            Type = type;
            Duration = duration;
            Message = message;
            StackTrace = stackTrace;
        }
    }
}