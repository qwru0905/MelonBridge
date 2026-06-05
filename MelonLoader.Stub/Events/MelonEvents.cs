namespace MelonLoader
{
    public static class MelonEvents
    {
        public static readonly MelonEvent OnApplicationEarlyStart = new MelonEvent();
        public static readonly MelonEvent OnApplicationStart = new MelonEvent();
        public static readonly MelonEvent OnApplicationLateStart = new MelonEvent();
        public static readonly MelonEvent OnApplicationQuit = new MelonEvent();
        public static readonly MelonEvent OnUpdate = new MelonEvent();
        public static readonly MelonEvent OnFixedUpdate = new MelonEvent();
        public static readonly MelonEvent OnLateUpdate = new MelonEvent();
        public static readonly MelonEvent OnGUI = new MelonEvent();
    }
}
