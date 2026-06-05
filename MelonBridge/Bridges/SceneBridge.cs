using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace MelonBridge.Bridges
{
    public static class SceneBridge
    {
        private static List<MelonBase> _mods = new List<MelonBase>();

        public static void Attach(List<MelonBase> mods)
        {
            _mods = mods;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public static void Detach()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _mods.Clear();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var mod in _mods)
            {
                try { mod.OnSceneWasLoaded(scene.buildIndex, scene.name); }
                catch (Exception e) { MelonLogger.Error($"[{mod.Info?.Name}] OnSceneWasLoaded: {e}"); }

                try { mod.OnSceneWasInitialized(scene.buildIndex, scene.name); }
                catch (Exception e) { MelonLogger.Error($"[{mod.Info?.Name}] OnSceneWasInitialized: {e}"); }
            }
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            foreach (var mod in _mods)
            {
                try { mod.OnSceneWasUnloaded(scene.buildIndex, scene.name); }
                catch (Exception e) { MelonLogger.Error($"[{mod.Info?.Name}] OnSceneWasUnloaded: {e}"); }
            }
        }
    }
}
