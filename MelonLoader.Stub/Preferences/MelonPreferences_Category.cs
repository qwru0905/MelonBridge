using System.Collections.Generic;

namespace MelonLoader
{
    public class MelonPreferences_Category
    {
        public string Identifier { get; }
        public string DisplayName { get; }

        internal readonly List<object> Entries = new List<object>();

        internal MelonPreferences_Category(string identifier, string displayName)
        {
            Identifier = identifier;
            DisplayName = displayName ?? identifier;
        }

        public MelonPreferences_Entry<T> CreateEntry<T>(string identifier, T defaultValue, string displayName = null)
        {
            var entry = new MelonPreferences_Entry<T>(identifier, displayName ?? identifier, defaultValue);
            Entries.Add(entry);
            MelonPreferences.Backend?.RegisterEntry(Identifier, entry);
            return entry;
        }
    }
}
