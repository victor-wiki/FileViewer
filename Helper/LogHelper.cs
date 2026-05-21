using FileViewer.Model;

namespace FileViewer.Helper
{
    public class LogHelper
    {
        public static LogType LogType { get; set; }
        private static object obj = new object();

        public static void LogInfo(string message, string logFilePath = null)
        {
            Log(LogType.Info, message, logFilePath);
        }

        public static void LogError(string message, string logFilePath = null)
        {
            Log(LogType.Error, message, logFilePath);
        }

        private static void Log(LogType logType, string message, string logFilePath = null)
        {
            string filePath = logFilePath;

            if(string.IsNullOrEmpty(logFilePath))
            {
                string logFolder = "log";

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                filePath = Path.Combine(logFolder, DateTime.Today.ToString("yyyyMMdd") + ".txt");
            }           

            string content = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}({logType}):{message}";

            lock (obj)
            {
                File.AppendAllLines(filePath, new string[] { content });
            }
        }        
    }
}
