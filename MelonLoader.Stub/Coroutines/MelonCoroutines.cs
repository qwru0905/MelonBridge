using System.Collections;
using UnityEngine;

namespace MelonLoader
{
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
