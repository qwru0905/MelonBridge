// Minimal stub for UnityModManager - only includes types needed for MelonBridge
using System;

namespace UnityModManagerNet
{
    public static class UnityModManager
    {
        public class ModEntry
        {
            public ModLogger Logger { get; }
            public string Path { get; }

            public Action<ModEntry, float> OnUpdate;
            public Action<ModEntry, float> OnFixedUpdate;
            public Action<ModEntry, float> OnLateUpdate;
            public Action<ModEntry> OnGUI;
            public Func<ModEntry, bool, bool> OnToggle;

            public ModEntry(string path)
            {
                Path = path;
                Logger = new ModLogger(System.IO.Path.GetFileName(path));
            }

            public sealed class ModLogger
            {
                private readonly string _prefix;
                public ModLogger(string prefix) => _prefix = prefix;
                public void Log(string str) { }
                public void Warning(string str) { }
                public void Error(string str) { }
            }
        }
    }
}
