using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace MelonBridge
{
    public static class ModLoader
    {
        public static List<MelonBase> LoadAll(string modsFolder)
        {
            if (!Directory.Exists(modsFolder))
            {
                MelonLogger.Warning($"MelonMods 폴더를 찾을 수 없음: {modsFolder}");
                return new List<MelonBase>();
            }

            var allMods = new List<(MelonBase mod, int priority)>();

            foreach (var dllPath in Directory.GetFiles(modsFolder, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);
                    var infoAttr = assembly.GetCustomAttribute<MelonInfoAttribute>();
                    if (infoAttr == null) continue;

                    if (ShouldSkipForPlatform(assembly)) continue;

                    var types = FindMelonTypes(assembly);
                    foreach (var type in types)
                    {
                        var mod = CreateInstance(type);
                        InjectInfo(mod, infoAttr);
                        var priority = assembly.GetCustomAttribute<MelonPriorityAttribute>()?.Priority ?? 0;
                        allMods.Add((mod, priority));
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"모드 로드 실패 [{Path.GetFileName(dllPath)}]: {e}");
                }
            }

            return allMods
                .OrderBy(x => x.mod is MelonPlugin ? 0 : 1)
                .ThenBy(x => x.priority)
                .Select(x => x.mod)
                .ToList();
        }

        public static List<Type> FindMelonTypes(Assembly assembly)
        {
            var result = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && (
                    typeof(MelonMod).IsAssignableFrom(type) ||
                    typeof(MelonPlugin).IsAssignableFrom(type)))
                {
                    result.Add(type);
                }
            }
            return result;
        }

        public static MelonBase CreateInstance(Type type)
        {
            return (MelonBase)Activator.CreateInstance(type);
        }

        public static void InjectInfo(MelonBase mod, MelonInfoAttribute info)
        {
            mod.Info = info;
            mod.LoggerInstance = new MelonLogger.Instance(info.Name);
        }

        public static List<(MelonBase mod, int priority)> SortByPriority(
            List<(MelonBase mod, int priority)> mods)
        {
            return mods.OrderBy(x => x.priority).ToList();
        }

        private static bool ShouldSkipForPlatform(Assembly assembly)
        {
            var domainAttr = assembly.GetCustomAttribute<MelonPlatformDomainAttribute>();
            if (domainAttr == null || domainAttr.Domain == MelonPlatformDomain.Any)
                return false;

            bool isIl2Cpp = Type.GetType("Il2CppSystem.Object, Il2CppMscorlib") != null;

            if (domainAttr.Domain == MelonPlatformDomain.Il2Cpp && !isIl2Cpp)
            {
                MelonLogger.Warning($"IL2CPP 전용 모드를 Mono 환경에서 스킵: {assembly.GetName().Name}");
                return true;
            }
            if (domainAttr.Domain == MelonPlatformDomain.Mono && isIl2Cpp)
            {
                MelonLogger.Warning($"Mono 전용 모드를 IL2CPP 환경에서 스킵: {assembly.GetName().Name}");
                return true;
            }
            return false;
        }
    }
}
