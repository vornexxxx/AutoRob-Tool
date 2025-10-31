using System;
using System.IO;

namespace ELRCRobTool
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ERLC_Log.txt");
        private static readonly object Lock = new object();

        // Add event to notify when a new log is available
        public static event Action<string>? OnLogMessage;

        public static void WriteLine(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(timestampedMessage); // Still print to console
            lock (Lock) // Ensure thread-safety when writing to file
            {
                File.AppendAllText(LogFilePath, timestampedMessage + Environment.NewLine);
            }
            // Send message to the event so the UI can update
            OnLogMessage?.Invoke(timestampedMessage);
        }

        public static void ClearLog()
        {
            lock (Lock)
            {
                if (File.Exists(LogFilePath))
                    File.WriteAllText(LogFilePath, string.Empty);
            }
        }
    }
}