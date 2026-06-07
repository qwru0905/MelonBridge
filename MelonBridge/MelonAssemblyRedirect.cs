using System;
using System.Linq;
using System.Reflection;

namespace MelonBridge
{
    // Melon mods are precompiled against the public HarmonyX/MonoMod identities
    // (e.g. "0Harmony, Version=2.10.2.0, PublicKeyToken=null"), but MelonBridge ships
    // private "shaded" copies of those same builds renamed to "*.Melon" so they can
    // coexist with UMM's own bundled Harmony (a different, older version that JALib
    // and other UMM-native mods depend on) in the same AppDomain.
    //
    // This redirects ONLY requests for the exact old identity/version that the shaded
    // copies were built from to the already-loaded shaded assembly, so type identity
    // (HarmonyLib.Harmony, MonoMod.Utils.* etc.) matches between MelonBridge,
    // MelonLoader.Stub and Melon mods. Requests for other versions (e.g. JALib's
    // "0Harmony, Version=2.3.6.0") fall through untouched and resolve to UMM's copy.
    internal static class MelonAssemblyRedirect
    {
        private static readonly (string OldName, Version OldVersion, string ShadedName)[] Map =
        {
            ("0Harmony", new Version(2, 10, 2, 0), "0Harmony.Melon"),
            ("MonoMod.Utils", new Version(22, 3, 23, 4), "MonoMod.Utils.Melon"),
            ("MonoMod.RuntimeDetour", new Version(22, 3, 23, 4), "MonoMod.RuntimeDetour.Melon"),
        };

        private static bool _installed;

        internal static void Install()
        {
            if (_installed) return;
            _installed = true;
            AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
        }

        private static Assembly? OnResolve(object? sender, ResolveEventArgs args)
        {
            AssemblyName requested;
            try { requested = new AssemblyName(args.Name); }
            catch { return null; }

            foreach (var (oldName, oldVersion, shadedName) in Map)
            {
                if (requested.Name != oldName) continue;
                if (requested.Version != null && requested.Version != oldVersion) continue;

                var shaded = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == shadedName);
                if (shaded == null) continue;

                MelonLoader.MelonLogger.Msg($"MelonAssemblyRedirect: {args.Name} -> {shaded.GetName().Name}");
                return shaded;
            }

            return null;
        }
    }
}
