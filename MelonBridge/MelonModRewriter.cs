using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Mono.Cecil;

namespace MelonBridge
{
    // Precompiled Melon mods reference the public "0Harmony"/"MonoMod.*" identities
    // (e.g. "0Harmony, Version=2.10.2.0"), but those simple names are already claimed
    // in this AppDomain by UMM's bundled Harmony 2.3.6 (loaded for JALib before
    // MelonBridge even starts). Weakly-named assembly resolution reuses whatever is
    // already loaded under a matching simple name regardless of the requested version,
    // so AssemblyResolve never fires and "HarmonyLib.Harmony" ends up resolving to the
    // wrong assembly -- producing MissingMethodException at the call sites.
    //
    // The fix: rewrite each mod's AssemblyRef table (via Mono.Cecil, in a cached copy on
    // disk so Assembly.Location/LoadFrom semantics are preserved) so it points directly
    // at our uniquely-named shaded copies (already loaded with those exact identities).
    // That sidesteps the simple-name collision entirely -- no redirect race involved.
    internal static class MelonModRewriter
    {
        private static readonly Dictionary<string, string> Rename = new Dictionary<string, string>
        {
            ["0Harmony"] = "0Harmony.Melon",
            ["MonoMod.Utils"] = "MonoMod.Utils.Melon",
            ["MonoMod.RuntimeDetour"] = "MonoMod.RuntimeDetour.Melon",
        };

        public static string PrepareForLoad(string dllPath, string cacheDir)
        {
            byte[] original;
            try { original = File.ReadAllBytes(dllPath); }
            catch { return dllPath; }

            byte[]? rewritten;
            try { rewritten = RewriteIfNeeded(original); }
            catch (Exception e)
            {
                MelonLogger.Warning($"MelonModRewriter: {Path.GetFileName(dllPath)} 분석 실패, 원본 사용: {e.Message}");
                return dllPath;
            }

            if (rewritten == null) return dllPath;

            try
            {
                Directory.CreateDirectory(cacheDir);
                var cachedPath = Path.Combine(cacheDir, Path.GetFileName(dllPath));
                File.WriteAllBytes(cachedPath, rewritten);
                MelonLogger.Msg($"MelonModRewriter: {Path.GetFileName(dllPath)}의 0Harmony/MonoMod 참조를 shaded 사본으로 재작성");
                return cachedPath;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"MelonModRewriter: {Path.GetFileName(dllPath)} 캐시 기록 실패, 원본 사용: {e.Message}");
                return dllPath;
            }
        }

        private static byte[]? RewriteIfNeeded(byte[] original)
        {
            using var input = new MemoryStream(original);
            using var asm = AssemblyDefinition.ReadAssembly(input);

            var changed = false;
            foreach (var module in asm.Modules)
            {
                foreach (var asmRef in module.AssemblyReferences)
                {
                    if (!Rename.TryGetValue(asmRef.Name, out var shadedName)) continue;
                    var shaded = FindLoadedShaded(shadedName);
                    if (shaded == null) continue;
                    asmRef.Name = shaded.Name;
                    asmRef.Version = shaded.Version;
                    changed = true;
                }
            }

            if (!changed) return null;

            using var output = new MemoryStream();
            asm.Write(output);
            return output.ToArray();
        }

        private static AssemblyName? FindLoadedShaded(string shadedName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName())
                .FirstOrDefault(n => n.Name == shadedName);
        }
    }
}
