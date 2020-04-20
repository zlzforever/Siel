using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Siel.Scheduler
{
    public class DependencyInjectionTaskFactory : TaskFactoryBase
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyInjectionTaskFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override TaskBase Create(Type type, string json)
        {
            using var scope = _serviceProvider.CreateScope();
            var obj = scope.ServiceProvider.GetService(type);
            if (obj == null)
            {
                throw new ApplicationException($"Create task {type.FullName} object by DI failed");
            }

            var task = (TaskBase) obj;

            // workaround to load configuration properties
            var origin = (TaskBase) JsonConvert.DeserializeObject(json, type);
            task.Load(origin);

            return task;
        }
    }
}