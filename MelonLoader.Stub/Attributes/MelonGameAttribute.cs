using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class MelonGameAttribute : Attribute
    {
        public string Developer { get; }
        public string Name { get; }

        public MelonGameAttribute(string developer = null, string name = null)
        {
            Developer = developer;
            Name = name;
        }
    }
}
