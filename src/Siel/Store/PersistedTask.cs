using System;
using System.Collections.Generic;
using HWT;
using Newtonsoft.Json;
using Siel.Common;

namespace Siel.Store
{
    public class PersistedTask
    {
        private readonly IReadOnlyDictionary<string, string> _emptyDict = new Dictionary<string, string>();

        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public string TypeName { get; private set; }

        /// <summary>
        /// 任务序列化数据
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// 任务数据
        /// </summary>
        public string Properties { get; private set; }

        /// <summary>
        /// 执行成功数
        /// </summary>
        public int SuccessCount { get; private set; }

        /// <summary>
        /// 执行失败数
        /// </summary>
        public int FailureCount { get; private set; }

        /// <summary>
        /// 任务创建时间
        /// </summary>
        public DateTime CreationTime { get; private set; }

        private PersistedTask()
        {
        }

        public PersistedTask(string name, ITimerTask task, Dictionary<string, string> properties = null) : this(null,
            name, task, properties)
        {
        }

        public PersistedTask(string id, string name, ITimerTask task, Dictionary<string, string> properties = null)
        {
            Id = string.IsNullOrWhiteSpace(id) ? CombGuid.NewGuid().ToString() : id;
            Name = name;
            CreationTime = DateTime.UtcNow;
            TypeName = task.GetType().AssemblyQualifiedName;
            Data = JsonConvert.SerializeObject(task);
            if (properties != null)
            {
                Properties = JsonConvert.SerializeObject(properties);
            }
        }

        public void Success()
        {
            SuccessCount += 1;
        }

        public void Fail()
        {
            FailureCount += 1;
        }

        public IReadOnlyDictionary<string, string> GetProperties()
        {
            if (string.IsNullOrWhiteSpace(Properties))
            {
                return _emptyDict;
            }

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties);
        }
    }
}