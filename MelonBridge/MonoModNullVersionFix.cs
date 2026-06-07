using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;

namespace MelonBridge
{
    // MonoMod.Utils crashes when resolving a type whose AssemblyNameReference.Version is null
    // (common in Unity assemblies with stripped metadata). This prefix guards that method.
    internal static class MonoModNullVersionFix
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            var monoModUtils = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "MonoMod.Utils.Melon");
            if (monoModUtils == null) { MelonLoader.MelonLogger.Warning("MonoModNullVersionFix: MonoMod.Utils.Melon not loaded"); return; }
            var reflHelperType = monoModUtils.GetType("MonoMod.Utils.ReflectionHelper");
            if (reflHelperType == null) return;

            MethodInfo target = null;
            foreach (var m in reflHelperType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public))
            {
                if (m.Name != "GetRuntimeHashedFullName") continue;
                var prms = m.GetParameters();
                if (prms.Length == 1 && prms[0].ParameterType == typeof(AssemblyNameReference))
                {
                    target = m;
                    break;
                }
            }

            if (target == null)
            {
                MelonLoader.MelonLogger.Warning("MonoModNullVersionFix: target method not found, skipping");
                return;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(MonoModNullVersionFix), nameof(Prefix)));
            MelonLoader.MelonLogger.Msg("MonoModNullVersionFix: patched GetRuntimeHashedFullName");
        }

        static int _callCount = 0;

        static bool Prefix(AssemblyNameReference asm, ref string __result)
        {
            if (System.Threading.Interlocked.Increment(ref _callCount) == 1)
                MelonLoader.MelonLogger.Msg("MonoModNullVersionFix: prefix invoked (first call)");

            // Cecil returns "" (not null) for neutral culture / unnamed refs, so ?? doesn't catch it
            // and AssemblyName(...) rejects the resulting "Culture=" / leading-comma strings as invalid.
            var name = string.IsNullOrEmpty(asm?.Name) ? "unknown" : asm.Name;
            string version;
            try { version = asm?.Version?.ToString() ?? "0.0.0.0"; }
            catch { version = "0.0.0.0"; }
            var culture = string.IsNullOrEmpty(asm?.Culture) ? "neutral" : asm.Culture;
            string pktStr;
            try
            {
                var pkt = asm?.PublicKeyToken;
                pktStr = (pkt == null || pkt.Length == 0)
                    ? "null"
                    : BitConverter.ToString(pkt).Replace("-", "").ToLowerInvariant();
            }
            catch { pktStr = "null"; }
            __result = $"{name}, Version={version}, Culture={culture}, PublicKeyToken={pktStr}";
            return false; // always use safe replacement; never call original
        }
    }
}
