using System;
using System.Collections.Generic;
using HWT;
using Microsoft.Extensions.Logging;

namespace Siel
{
    public interface ITask : ITimerTask
    {
        void Remove();
    }
}