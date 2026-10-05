using UnityEngine;

namespace HeliosDivide
{
    /// <summary>
    /// Lightweight development logging for Helios Divide.
    /// Keep Info rare; use Warn/Error for recoverable and blocking issues.
    /// </summary>
    public static class HDLog
    {
        public const string Prefix = "[HeliosDivide]";

        public static void Info(string message, Object context = null)
        {
            Debug.Log($"{Prefix} {message}", context);
        }

        public static void Warn(string message, Object context = null)
        {
            Debug.LogWarning($"{Prefix} {message}", context);
        }

        public static void Error(string message, Object context = null)
        {
            Debug.LogError($"{Prefix} {message}", context);
        }
    }
}
