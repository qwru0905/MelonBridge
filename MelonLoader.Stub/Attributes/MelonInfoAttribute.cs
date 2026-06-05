using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonInfoAttribute : Attribute
    {
        public Type SystemType { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; }
        public string DownloadLink { get; }

        public MelonInfoAttribute(Type systemType, string name, string version, string author, string downloadLink = null)
        {
            SystemType = systemType;
            Name = name;
            Version = version;
            Author = author;
            DownloadLink = downloadLink;
        }
    }
}
