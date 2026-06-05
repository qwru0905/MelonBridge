using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class MelonIncompatibleAssembliesAttribute : Attribute
    {
        public string[] AssemblyNames { get; }

        public MelonIncompatibleAssembliesAttribute(params string[] assemblyNames)
        {
            AssemblyNames = assemblyNames;
        }
    }
}
