using MelonLoader;

namespace MelonBridge.Bridges
{
    public static class LoggerBridge
    {
        public interface IUmmLogger
        {
            void Log(string msg);
            void Warning(string msg);
            void Error(string msg);
        }

        public static void Attach(IUmmLogger ummLogger)
        {
            MelonLogger.Handler = new UmmLogHandler(ummLogger);
        }

        public static void Detach()
        {
            MelonLogger.Handler = new MelonLogger.NullLogHandler();
        }

        private sealed class UmmLogHandler : MelonLogger.ILogHandler
        {
            private readonly IUmmLogger _logger;
            public UmmLogHandler(IUmmLogger logger) => _logger = logger;
            public void Msg(string msg) => _logger.Log(msg);
            public void Warning(string msg) => _logger.Warning(msg);
            public void Error(string msg) => _logger.Error(msg);
        }
    }
}
