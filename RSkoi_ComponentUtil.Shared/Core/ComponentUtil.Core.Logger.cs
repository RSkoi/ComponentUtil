using BepInEx.Logging;

namespace RSkoi_ComponentUtil
{
    internal static class Logger
    {
        static ManualLogSource _logger;

        internal static void Init(ManualLogSource logger)   => _logger = logger;

        internal static void LogInfo(object data)           => _logger?.LogInfo(data);
        internal static void LogMessage(object data)        => _logger?.LogMessage(data);
        internal static void LogWarning(object data)        => _logger?.LogWarning(data);
        internal static void LogError(object data)          => _logger?.LogError(data);
    }
}
