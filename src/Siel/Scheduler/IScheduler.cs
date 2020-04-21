using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Siel.Common;
using Siel.Store;

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
        Task<PagedResult<PersistedTask>> PagedQueryAsync(string keyword, int page, int limit);
        Task<SielStatus> GetStatusAsync();
    }
}