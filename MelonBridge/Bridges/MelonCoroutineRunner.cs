using UnityEngine;

namespace MelonBridge.Bridges
{
    public sealed class MelonCoroutineRunner : MonoBehaviour
    {
        private void Awake()
        {
            Object.DontDestroyOnLoad(gameObject);
            MelonLoader.MelonCoroutines.Runner = this;
        }

        private void OnGUI()
        {
            Main.OnUnityGUI();
        }
    }
}
