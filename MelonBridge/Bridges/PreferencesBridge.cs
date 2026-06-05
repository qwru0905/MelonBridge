using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using Tomlet;
using Tomlet.Models;

namespace MelonBridge.Bridges
{
    public static class PreferencesBridge
    {
        private static TomlBackend _backend;

        public static void Attach(string cfgPath)
        {
            _backend = new TomlBackend(cfgPath);
            MelonPreferences.Backend = _backend;
        }

        public static void Detach()
        {
            MelonPreferences.Backend = null;
            _backend = null;
        }

        private sealed class TomlBackend : MelonPreferences.IPreferencesBackend
        {
            private readonly string _path;
            private readonly List<(string catId, string entryId, Action<TomlValue> setter, Func<TomlValue> getter)> _reg
                = new List<(string, string, Action<TomlValue>, Func<TomlValue>)>();

            public TomlBackend(string path) => _path = path;

            public void RegisterCategory(MelonPreferences_Category category) { }

            public void RegisterEntry<T>(string categoryId, MelonPreferences_Entry<T> entry)
            {
                _reg.Add((
                    categoryId,
                    entry.Identifier,
                    tomlVal =>
                    {
                        try { entry.Value = TomletMain.To<T>(tomlVal); }
                        catch { }
                    },
                    () => TomletMain.ValueFrom(entry.Value)
                ));
            }

            public void Load()
            {
                if (!File.Exists(_path)) return;
                TomlDocument doc;
                try { doc = TomlParser.ParseFile(_path); }
                catch { return; }

                foreach (var (catId, entryId, setter, _) in _reg)
                {
                    if (doc.TryGetValue(catId, out var catVal) &&
                        catVal is TomlTable catTable &&
                        catTable.TryGetValue(entryId, out var entryVal))
                    {
                        setter(entryVal);
                    }
                }
            }

            public void Save()
            {
                TomlDocument doc;
                try { doc = File.Exists(_path) ? TomlParser.ParseFile(_path) : new TomlParser().Parse(""); }
                catch { doc = new TomlParser().Parse(""); }

                foreach (var (catId, entryId, _, getter) in _reg)
                {
                    if (!doc.ContainsKey(catId))
                        doc.PutValue(catId, new TomlTable(), true);

                    if (doc.TryGetValue(catId, out var catVal) && catVal is TomlTable catTable)
                        catTable.PutValue(entryId, getter(), true);
                }
                File.WriteAllText(_path, doc.SerializedValue);
            }
        }
    }
}
