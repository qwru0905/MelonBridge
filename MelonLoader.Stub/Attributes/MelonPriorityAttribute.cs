using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonPriorityAttribute : Attribute
    {
        public int Priority { get; }

        public MelonPriorityAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}
