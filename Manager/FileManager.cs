namespace FileViewer.Manager
{
    public class FileManager
    {
        public static string DataRootFolder
        {
            get
            {
                return FileSystem.Current.AppDataDirectory;
            }
        }

        public static string CacheRootFolder
        {
            get
            {
                return FileSystem.Current.CacheDirectory;
            }
        }
    }
}
