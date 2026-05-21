using FileViewer.Helper;

namespace FileViewer.Manager
{
    public class LogManager: FileManager
    {
        private readonly static string logFileNameFormat = "log_{0}.txt";

        private static string logFolderName => "log";
        private static bool IsEnableLog => SettingManager.GetSetting().EnableLog;
        public static string LogFolder => Path.Combine(DataRootFolder, logFolderName);

        internal static string LogFilePath
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    throw new NotImplementedException();
                }

                return Path.Combine(DataRootFolder, logFolderName, string.Format(logFileNameFormat, DateTime.Today.ToString("yyyyMMdd")));
            }
        }

        static LogManager()
        {
            string folder = LogFolder;

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        public static void LogInfo(string message)
        {
            if (IsEnableLog)
            {
                LogHelper.LogInfo(message, LogFilePath);
            }
        }

        public static void LogError(string message)
        {
            if (IsEnableLog)
            {
                LogHelper.LogError(message, LogFilePath);
            }
        }

        public static void LogException(Exception ex)
        {
            if (!IsEnableLog)
            {
                return;
            }

            Page currentPage = Shell.Current?.CurrentPage;

            string prefix = "";

            if (currentPage != null)
            {
                prefix = currentPage.ToString() + Environment.NewLine;
            }

            string message = $"{prefix}{ExceptionHelper.GetExceptionDetails(ex)}";

            LogHelper.LogError(message, LogFilePath);
        }        
    }
}
