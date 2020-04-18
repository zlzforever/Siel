using System.Collections.Generic;
using HWT;
using Microsoft.Extensions.Logging;

namespace Siel
{
    public interface ITask : ITimerTask
    {
        void Remove();
        void Initialize(string id, string name, Dictionary<string, string> properties);
        void UseLoggerFactory(ILoggerFactory loggerFactory);
        long GetNextTimeSpan();
        void Verify();
    }
}