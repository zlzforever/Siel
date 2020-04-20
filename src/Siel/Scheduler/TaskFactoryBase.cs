using System;

namespace Siel.Scheduler
{
    public abstract class TaskFactoryBase : ITaskFactory
    {
        private static readonly Type TaskBaseType = typeof(TaskBase);

        public TaskBase Create(string typeName, string data)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            var type = Type.GetType(typeName);
            if (type == null)
            {
                throw new ApplicationException($"Get task type {typeName} failed");
            }

            if (!TaskBaseType.IsAssignableFrom(type))
            {
                throw new ApplicationException($"Type {type.FullName} isn't a valid task");
            }

            return Create(type, data);
        }

        protected abstract TaskBase Create(Type type, string json);
    }
}