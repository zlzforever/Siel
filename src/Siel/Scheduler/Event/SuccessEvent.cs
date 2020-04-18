namespace Siel.Scheduler.Event
{
    public struct SuccessEvent  
    {
        public string Id { get; private set; }

        public SuccessEvent(string id)
        {
            Id = id;
        }
    }
}