using FileViewer.Model;

namespace FileViewer.Helper
{
    public class FileHelper
    {
        public static FileOpenMode GetFileOpenModeByExtension(string extension)
        {
            FileOpenMode openMode = FileOpenMode.Unknown;

            switch (extension)
            {
                case ".pdf":
                    openMode = FileOpenMode.ByPdfPath;
                    break;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    openMode = FileOpenMode.ByImage;
                    break;
                case ".mp3":
                case ".wav":
                case ".m4a":
                case ".wma":
                case ".flac":
                case ".mp4":
                case ".avi":
                case ".wmv":
                case ".flv":
                case ".mkv":
                case ".mov":
                case ".mpg":
                case ".ogg":
                case ".vob":
                    openMode = FileOpenMode.ByMediaPath;
                    break;
                case ".doc":
                case ".docx":
                    openMode = FileOpenMode.ByWordParser;
                    break;
                case ".xls":
                case ".xlsx":
                    openMode = FileOpenMode.ByExcelParser;
                    break;
                case ".csv":
                    openMode = FileOpenMode.ByCsvParser;
                    break;
                case ".db3":
                case ".db":
                    openMode = FileOpenMode.BySqlite;
                    break;
                case ".mdb":
                case ".accdb":
                    if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    {
                        openMode = FileOpenMode.ByAccess;
                    }
                    break;
                case ".zip":
                case ".gz":
                case ".7z":
                case ".rar":
                    openMode = FileOpenMode.ByZip;              
                    break;
                default:

                    break;
            }

            return openMode;
        }

        public static FileOpenMode DetectFileOpenMode(string filePath)
        {
            if (IsPlainTextFile(filePath))
            {
                return FileOpenMode.ByTextContent;
            }
            else
            {
                return FileOpenMode.Unknown;
            }
        }

        public static FileOpenMode DetectFileOpenMode(Stream stream, bool disposeAfterUsed = true)
        {
            if (IsPlainTextFile(stream, disposeAfterUsed))
            {
                return FileOpenMode.ByTextContent;
            }
            else
            {
                return FileOpenMode.Unknown;
            }
        }

        public static bool IsPlainTextFile(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                return IsPlainTextFile(fs);
            }
        }

        public static bool IsPlainTextFile(Stream stream, bool disposeAfterUsed = true)
        {
            var reader = new StreamReader(stream);

            var buffer = new char[4096];
            var bytesRead = reader.Read(buffer, 0, buffer.Length);

            bool isPlianText = true;

            for (int i = 0; i < bytesRead; i++)
            {
                if (buffer[i] == '\0')
                {
                    return false;
                }

                if (char.IsControl(buffer[i]) &&
                    buffer[i] != '\r' &&
                    buffer[i] != '\n' &&
                    buffer[i] != '\t')
                {
                    isPlianText = false;

                    break;
                }
            }

            if (disposeAfterUsed)
            {
                reader.Close();
                reader.Dispose();
                stream.Dispose();
            }
            else
            {
                stream.Seek(0, SeekOrigin.Begin);
            }               

            return isPlianText;
        }
    }
}
