using System.Collections.Generic;
using System.Threading.Tasks;
using Siel.Common;

namespace Siel.Store
{
    public interface ITaskStore
    {
        ValueTask<bool> SaveAsync(PersistedTask task);
        Task<IEnumerable<PersistedTask>> TakeAsync(int page, int limit);
        ValueTask<bool> RemoveAsync(string id);
        Task<PagedResult<PersistedTask>> PagedQueryAsync(string keyword, int page, int limit);
    }
}