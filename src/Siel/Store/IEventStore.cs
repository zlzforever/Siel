using System.Threading.Tasks;
using Siel.Scheduler.Event;

namespace Siel.Store
{
    public interface IEventStore
    {
        Task SaveFailureAsync(FailureEvent @event);
        Task SaveSuccessAsync(SuccessEvent @event);
    }
}