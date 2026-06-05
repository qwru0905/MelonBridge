using System;

namespace MelonLoader
{
    public enum MelonPlatformDomain
    {
        Any = 0,
        Mono = 1,
        Il2Cpp = 2,
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonPlatformDomainAttribute : Attribute
    {
        public MelonPlatformDomain Domain { get; }

        public MelonPlatformDomainAttribute(MelonPlatformDomain domain = MelonPlatformDomain.Any)
        {
            Domain = domain;
        }
    }
}
