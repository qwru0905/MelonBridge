#if !UNITY_EDITOR && !UNITY_STANDALONE
using System.Collections;

namespace UnityEngine
{
    // Stub types for compilation in non-Unity environments
    public class Object
    {
        public override string ToString() => base.ToString();
    }

    public class Coroutine : Object { }

    public class MonoBehaviour : Object
    {
        public Coroutine StartCoroutine(System.Collections.IEnumerator routine) => null;
        public void StopCoroutine(Coroutine coroutine) { }
    }
}
#else
using System.Collections;
using UnityEngine;
#endif

namespace MelonLoader
{
    using System.Collections;
    using UnityEngine;

    public static class MelonCoroutines
    {
        internal static MonoBehaviour Runner;

        public static Coroutine Start(IEnumerator routine)
        {
            if (Runner == null)
            {
                MelonLogger.Warning("MelonCoroutines: Runner not initialized. Coroutine skipped.");
                return null;
            }
            return Runner.StartCoroutine(routine);
        }

        public static void Stop(Coroutine coroutine)
        {
            if (Runner == null || coroutine == null) return;
            Runner.StopCoroutine(coroutine);
        }
    }
}
