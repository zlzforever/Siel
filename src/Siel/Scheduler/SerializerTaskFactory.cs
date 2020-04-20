using System;
using Newtonsoft.Json;

namespace Siel.Scheduler
{
    public class SerializerTaskFactory : TaskFactoryBase
    {
        protected override TaskBase Create(Type type, string json)
        {
            var task = JsonConvert.DeserializeObject(json, type);
            if (task == null)
            {
                throw new ApplicationException($"Create task {type.FullName} object by Serializer failed");
            }

            return (TaskBase) task;
        }
    }
}