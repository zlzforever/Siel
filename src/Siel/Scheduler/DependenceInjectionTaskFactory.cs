using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Siel.Scheduler
{
    public class DependenceInjectionTaskFactory : ITaskFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DependenceInjectionTaskFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TaskBase Create(string typeName, string json)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            var type = Type.GetType(typeName);
            if (type == null)
            {
                return null;
            }

            using var scope = _serviceProvider.CreateScope();
            var obj = scope.ServiceProvider.GetService(type);
            if (obj == null)
            {
                throw new ApplicationException("Create task object by DI failed");
            }

            var task = obj as TaskBase;
            if (task == null)
            {
                throw new ApplicationException($"{typeName} isn't a SielTask");
            }

            var origin = (TaskBase) JsonConvert.DeserializeObject(json, type);
            task.Load(origin);
            return task;
        }
    }
}