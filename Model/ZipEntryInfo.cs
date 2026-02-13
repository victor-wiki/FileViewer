using ICSharpCode.SharpZipLib.Zip;
using SharpCompress.Archives;

namespace FileViewer.Model
{
    public class ZipEntryInfo
    {
        public bool IsFile { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Icon => this.IsFile ? "file" : "folder";
        public string IconColor => this.IsFile ? "LightBlue" : "Orange";
    }
}
