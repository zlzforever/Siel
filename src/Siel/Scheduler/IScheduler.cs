using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Siel.Scheduler
{
    public interface IScheduler : IHostedService
    {
        ValueTask<string> NewAsync(string name, ITask task,
            Dictionary<string, string> properties = null);

        ValueTask<string> NewAsync(string id, string name, ITask task,
            Dictionary<string, string> properties = null);
        ValueTask<bool> UpdateAsync(string id, string name, ITask task,
            Dictionary<string, string> properties = null);
        ValueTask<bool> RemoveAsync(string id);
        ValueTask<bool> TriggerAsync(string id);
    }
}