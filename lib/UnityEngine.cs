// Minimal stub for UnityEngine.CoreModule - only includes types needed for MelonLoader.Stub

namespace UnityEngine
{
    public class Object
    {
        public override string ToString() => base.ToString();
        public static void DontDestroyOnLoad(Object target) { }
        public static void Destroy(Object obj) { }
    }

    public class Coroutine : Object { }

    public class MonoBehaviour : Object
    {
        public GameObject gameObject { get; }
        public Coroutine StartCoroutine(System.Collections.IEnumerator routine) => null;
        public void StopCoroutine(Coroutine coroutine) { }
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public T AddComponent<T>() where T : MonoBehaviour => null;
    }
}

namespace UnityEngine.SceneManagement
{
    public delegate void UnityAction<T1, T2>(T1 arg0, T2 arg1);
    public delegate void UnityAction<T1>(T1 arg0);

    public struct Scene
    {
        public int buildIndex { get; }
        public string name { get; }
    }

    public enum LoadSceneMode
    {
        Single = 0,
        Additive = 1
    }

    public static class SceneManager
    {
        public static event UnityAction<Scene, LoadSceneMode> sceneLoaded;
        public static event UnityAction<Scene> sceneUnloaded;

        static SceneManager()
        {
            sceneLoaded = null;
            sceneUnloaded = null;
        }
    }
}
