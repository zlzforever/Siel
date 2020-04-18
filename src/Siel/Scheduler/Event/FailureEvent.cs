namespace Siel.Scheduler.Event
{
    public struct FailureEvent
    {
        public string Id { get; private set; }
        public string StackTrace { get; private set; }

        public FailureEvent(string id, string stackTrace)
        {
            Id = id;
            StackTrace = stackTrace;
        }
    }
}