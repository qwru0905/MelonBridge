using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonBridge.Bridges;
using UnityEngine;
using UnityModManagerNet;

namespace MelonBridge
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        private static List<MelonBase> _mods = new List<MelonBase>();
        private static GameObject _runnerGo;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            MelonAssemblyRedirect.Install();

            LoggerBridge.Attach(new UmmLoggerAdapter(modEntry.Logger));

            var gameRoot = Path.GetFullPath(Path.Combine(modEntry.Path, "..", ".."));
            var cfgPath = Path.Combine(gameRoot, "UserData", "MelonPreferences.cfg");
            PreferencesBridge.Attach(cfgPath);

            var modsFolder = Path.Combine(gameRoot, "MelonMods");
            _mods = ModLoader.LoadAll(modsFolder);

            _runnerGo = new GameObject("MelonCoroutineRunner");
            _runnerGo.AddComponent<MelonCoroutineRunner>();

            MonoModNullVersionFix.Apply(new HarmonyLib.Harmony("MelonBridge.MonoModFix"));

            SceneBridge.Attach(_mods);

            modEntry.OnUpdate = OnUpdate;
            modEntry.OnFixedUpdate = OnFixedUpdate;
            modEntry.OnLateUpdate = OnLateUpdate;
            modEntry.OnToggle = OnToggle;

            InvokeAll(m => m.OnEarlyInitializeMelon(), "OnEarlyInitializeMelon");
            InvokeAll(m => m.OnInitializeMelon(), "OnInitializeMelon");
            InvokeAll(m => m.OnApplicationStart(), "OnApplicationStart");
            InvokeAll(m => m.OnApplicationLateStart(), "OnApplicationLateStart");

            MelonLogger.Msg($"MelonBridge: {_mods.Count}개 모드 로드 완료.");
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry entry, float dt)
            => InvokeAll(m => m.OnUpdate(), "OnUpdate");

        private static void OnFixedUpdate(UnityModManager.ModEntry entry, float dt)
            => InvokeAll(m => m.OnFixedUpdate(), "OnFixedUpdate");

        private static void OnLateUpdate(UnityModManager.ModEntry entry, float dt)
            => InvokeAll(m => m.OnLateUpdate(), "OnLateUpdate");

        internal static void OnUnityGUI()
            => InvokeAll(m => m.OnGUI(), "OnGUI");

        private static bool OnToggle(UnityModManager.ModEntry entry, bool active)
        {
            if (!active)
            {
                InvokeAll(m => m.OnDeinitializeMelon(), "OnDeinitializeMelon");
                SceneBridge.Detach();
                if (_runnerGo != null)
                {
                    UnityEngine.Object.Destroy(_runnerGo);
                    _runnerGo = null;
                    MelonLoader.MelonCoroutines.Runner = null;
                }
            }
            else
            {
                _runnerGo = new GameObject("MelonCoroutineRunner");
                _runnerGo.AddComponent<MelonCoroutineRunner>();
                SceneBridge.Attach(_mods);
                InvokeAll(m => m.OnInitializeMelon(), "OnInitializeMelon");
            }
            return true;
        }

        private static void InvokeAll(Action<MelonBase> action, string callbackName)
        {
            foreach (var mod in _mods)
            {
                try { action(mod); }
                catch (Exception e)
                {
                    MelonLogger.Error($"[{mod.Info?.Name ?? "?"}] {callbackName}: {e}");
                }
            }
        }

        private sealed class UmmLoggerAdapter : LoggerBridge.IUmmLogger
        {
            private readonly UnityModManager.ModEntry.ModLogger _logger;
            public UmmLoggerAdapter(UnityModManager.ModEntry.ModLogger logger) => _logger = logger;
            public void Log(string msg) => _logger.Log(msg);
            public void Warning(string msg) => _logger.Warning(msg);
            public void Error(string msg) => _logger.Error(msg);
        }
    }
}
