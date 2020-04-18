namespace Siel.Scheduler
{
    public interface ITaskFactory
    {
        SielTask Create(string typeName, string data);
    }
}