// Minimal stub for UnityEngine.CoreModule - only includes types needed for MelonLoader.Stub

namespace UnityEngine
{
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
