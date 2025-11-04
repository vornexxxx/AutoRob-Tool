using System;
using System.IO;

namespace ELRCRobTool
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ERLC_Log.txt");
        private static readonly string DebugLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ERLC_Debug_Log.txt");
        private static readonly object Lock = new object();

        public static event Action<string>? OnLogMessage;

        public static void WriteLine(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(timestampedMessage); // Still print to console
            lock (Lock)
            {
                File.AppendAllText(LogFilePath, timestampedMessage + Environment.NewLine);
            }
            OnLogMessage?.Invoke(timestampedMessage);
        }

        /// <summary>
        /// Writes a message to the debug log file if debug mode is enabled.
        /// </summary>
        public static void DebugWriteLine(string message)
        {
            if (!AppSettings.Config.DebugModeEnabled) return;

            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}] [DEBUG] {message}";
            lock (Lock)
            {
                File.AppendAllText(DebugLogFilePath, timestampedMessage + Environment.NewLine);
            }
        }

        public static void ClearLogs()
        {
            lock (Lock)
            {
                if (File.Exists(LogFilePath))
                    File.WriteAllText(LogFilePath, string.Empty);
                if (File.Exists(DebugLogFilePath))
                    File.WriteAllText(DebugLogFilePath, string.Empty);
            }
        }
    }
}