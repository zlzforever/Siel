namespace Siel.Scheduler
{
    public interface ITaskFactory
    {
        TaskBase Create(string typeName, string data);
    }
}