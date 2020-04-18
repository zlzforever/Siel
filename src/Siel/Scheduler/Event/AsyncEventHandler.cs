using System.Threading.Tasks;

namespace Siel.Scheduler.Event
{
    public delegate Task AsyncEventHandler<in TEvent>(TEvent @event);
}