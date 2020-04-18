namespace Siel.Scheduler.Event
{
    public class SuccessEvent  
    {
        public string Id { get; private set; }

        public SuccessEvent(string id)
        {
            Id = id;
        }
    }
}