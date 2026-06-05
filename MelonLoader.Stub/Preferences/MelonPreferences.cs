using System.Collections.Generic;

namespace MelonLoader
{
    public static class MelonPreferences
    {
        internal static IPreferencesBackend Backend;

        private static readonly Dictionary<string, MelonPreferences_Category> Categories = new Dictionary<string, MelonPreferences_Category>();

        public static MelonPreferences_Category CreateCategory(string identifier, string displayName = null)
        {
            if (!Categories.TryGetValue(identifier, out var category))
            {
                category = new MelonPreferences_Category(identifier, displayName);
                Categories[identifier] = category;
                Backend?.RegisterCategory(category);
            }
            return category;
        }

        public static MelonPreferences_Entry<T> CreateEntry<T>(
            string categoryIdentifier, string identifier, T defaultValue, string displayName = null)
        {
            var category = CreateCategory(categoryIdentifier);
            return category.CreateEntry(identifier, defaultValue, displayName);
        }

        public static void LoadAll() => Backend?.Load();
        public static void SaveAll() => Backend?.Save();

        internal interface IPreferencesBackend
        {
            void RegisterCategory(MelonPreferences_Category category);
            void RegisterEntry<T>(string categoryId, MelonPreferences_Entry<T> entry);
            void Load();
            void Save();
        }
    }
}
