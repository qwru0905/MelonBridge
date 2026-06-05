using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonProcessIdAttribute : Attribute
    {
        public string ProcessId { get; }

        public MelonProcessIdAttribute(string processId = null)
        {
            ProcessId = processId;
        }
    }
}
