using System;
using Newtonsoft.Json;

namespace Siel.Scheduler
{
    public class SerializerTaskFactory : ITaskFactory
    {
        public SielTask Create(string typeName, string json)
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

            try
            {
                var task = JsonConvert.DeserializeObject(json, type);
                return task as SielTask;
            }
            catch
            {
                return null;
            }
        }
    }
}