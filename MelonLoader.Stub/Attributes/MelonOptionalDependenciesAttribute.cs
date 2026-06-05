using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonOptionalDependenciesAttribute : Attribute
    {
        public string[] AssemblyNames { get; }

        public MelonOptionalDependenciesAttribute(params string[] assemblyNames)
        {
            AssemblyNames = assemblyNames;
        }
    }
}
