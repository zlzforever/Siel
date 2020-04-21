namespace Siel.Scheduler.Event
{
    public struct SuccessEvent
    {
        public string Id { get; private set; }
        public int Duration { get; private set; }
        public string Type { get; private set; }

        public SuccessEvent(string id, string type, int duration)
        {
            Id = id;
            Duration = duration;
            Type = type;
        }
    }
}