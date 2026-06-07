namespace MelonLoader
{
    public abstract class MelonBase
    {
        public MelonInfoAttribute Info { get; internal set; }
        public MelonLogger.Instance LoggerInstance { get; internal set; }

        private HarmonyLib.Harmony? _harmonyInstance;
        public HarmonyLib.Harmony HarmonyInstance =>
            _harmonyInstance ??= new HarmonyLib.Harmony(Info?.SystemType?.FullName ?? GetType().FullName);

        public virtual void OnEarlyInitializeMelon() { }
        public virtual void OnInitializeMelon() { }
        public virtual void OnDeinitializeMelon() { }
        public virtual void OnApplicationStart() { }
        public virtual void OnApplicationLateStart() { }
        public virtual void OnApplicationQuit() { }
        public virtual void OnApplicationDefiniteQuit() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnGUI() { }
        public virtual void OnPreferencesLoaded() { }
        public virtual void OnPreferencesSaved() { }
        public virtual void OnSceneWasLoaded(int buildIndex, string sceneName) { }
        public virtual void OnSceneWasInitialized(int buildIndex, string sceneName) { }
        public virtual void OnSceneWasUnloaded(int buildIndex, string sceneName) { }
    }
}
