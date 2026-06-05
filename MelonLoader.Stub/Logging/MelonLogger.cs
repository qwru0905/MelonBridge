using System;

namespace MelonLoader
{
    public static class MelonLogger
    {
        internal static ILogHandler Handler = new NullLogHandler();

        public static void Msg(string msg) => Handler.Msg(msg);
        public static void Msg(object obj) => Handler.Msg(obj?.ToString() ?? "null");
        public static void Msg(ConsoleColor color, string msg) => Handler.Msg(msg);
        public static void Warning(string msg) => Handler.Warning(msg);
        public static void Error(string msg) => Handler.Error(msg);
        public static void BigError(string msg) => Handler.Error($"== {msg} ==");

        internal interface ILogHandler
        {
            void Msg(string msg);
            void Warning(string msg);
            void Error(string msg);
        }

        internal sealed class NullLogHandler : ILogHandler
        {
            public void Msg(string msg) { }
            public void Warning(string msg) { }
            public void Error(string msg) { }
        }

        public sealed class Instance
        {
            public string Name { get; }

            public Instance(string name)
            {
                Name = name;
            }

            public void Msg(string msg) => Handler.Msg($"[{Name}] {msg}");
            public void Msg(object obj) => Msg(obj?.ToString() ?? "null");
            public void Warning(string msg) => Handler.Warning($"[{Name}] {msg}");
            public void Error(string msg) => Handler.Error($"[{Name}] {msg}");
        }
    }
}
