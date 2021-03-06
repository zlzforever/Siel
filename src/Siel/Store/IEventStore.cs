using System.Threading.Tasks;
using Siel.Scheduler.Event;

namespace Siel.Store
{
    public interface IEventStore
    {
        Task FailAsync(FailureEvent @event);
        Task SuccessAsync(SuccessEvent @event);
    }
}