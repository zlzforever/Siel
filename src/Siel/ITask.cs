using System;
using System.Collections.Generic;
using HWT;
using Microsoft.Extensions.Logging;

namespace Siel
{
    public interface ITask : ITimerTask
    {
        void Remove();
        void Initialize(string id, string name, IReadOnlyDictionary<string, string> properties);
        void UseLoggerFactory(ILoggerFactory loggerFactory);
        TimeSpan GetNextTimeSpan();
        void Verify();
    }
}