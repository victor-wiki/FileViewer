using FileViewer.Helper;
using FileViewer.Model;
using ICSharpCode.SharpZipLib.Core;
using SharpCompress.Archives;
using System.Text;

namespace FileViewer.Views;

public partial class Explorer : ContentPage
{
    private string filePath;
    private List<ZipEntryInfo> entryInfos = new List<ZipEntryInfo>();
    private string entryRootDirectory = null;
    private string currentEntryDirectory = null;
    public Explorer(string filePath)
    {
        InitializeComponent();

        this.filePath = filePath;

        this.Title = Path.GetFileName(filePath);

        this.ShowExplorer();
    }

    private void ShowExplorer()
    {
        using (var zipfile = ArchiveFactory.OpenArchive(this.filePath, this.GetZipOptions()))
        {
            foreach (var entry in zipfile.Entries)
            {
                string key = entry.Key;
                string name = Path.GetFileName(key.TrimEnd('/'));
                string path = entry.Key.TrimEnd('/');

                if (this.entryRootDirectory == null)
                {
                    if (path.Contains("/"))
                    {
                        int index = path.IndexOf("/");

                        this.entryRootDirectory = path.Substring(0, index);
                    }
                }

                ZipEntryInfo entryInfo = new ZipEntryInfo()
                {
                    IsFile = !entry.IsDirectory,
                    Key = key,
                    Name = name,
                    Path = path
                };

                this.entryInfos.Add(entryInfo);
            }

            this.lvEntries.ItemsSource = this.entryInfos.Where(item => Path.GetDirectoryName(item.Path) == (this.entryRootDirectory??"")).ToList();
        }
    }  
    
    private SharpCompress.Readers.ReaderOptions GetZipOptions()
    {
        var cultureInfo = System.Globalization.CultureInfo.CurrentCulture;

        SharpCompress.Readers.ReaderOptions options = new SharpCompress.Readers.ReaderOptions();

        Encoding defaultEncoding = Encoding.UTF8;

        if (cultureInfo.Name == "zh-CN")
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            defaultEncoding = Encoding.GetEncoding("gbk");
        }

        options.ArchiveEncoding.Default = defaultEncoding;

        return options;
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Grid grid = sender as Grid;

        ZipEntryInfo entryInfo = grid.BindingContext as ZipEntryInfo;

        if(!entryInfo.IsFile)
        {
            this.currentEntryDirectory = entryInfo.Path;

            var infos = this.entryInfos.Where(item => this.GetDirectoryPath(item.Path) == entryInfo.Path && item != entryInfo);

            this.lvEntries.ItemsSource = infos;

            this.SetToolbarItemStatus(this.tbiBack, true);
        }
        else
        {
            string extension = Path.GetExtension(entryInfo.Path).ToLower();

            FileOpenMode openMode = FileHelper.GetFileOpenModeByExtension(extension);
          
            using (var zipfile = ArchiveFactory.OpenArchive(this.filePath, this.GetZipOptions()))
            {
                var entry = zipfile.Entries.FirstOrDefault(item => item.Key == entryInfo.Key);

                using (Stream zipStream = await entry.OpenEntryStreamAsync())
                {
                    Stream ms = this.ConvertToMemoryStream(zipStream);

                    if (openMode == FileOpenMode.Unknown)
                    {
                        openMode = FileHelper.DetectFileOpenMode(ms, false);
                    }

                    if (openMode == FileOpenMode.Unknown)
                    {
                        await DisplayAlert("Information", "Not support.", "OK");
                        return;
                    }

                    if (openMode == FileOpenMode.ByTextContent)
                    {
                        TextViewer page = (TextViewer)Activator.CreateInstance(typeof(TextViewer), ms, entryInfo.Name);

                        await Navigation.PushAsync(page);
                    }
                    else if(openMode == FileOpenMode.ByWordParser)
                    {
                        WordViewer page = (WordViewer)Activator.CreateInstance(typeof(WordViewer), ms, entryInfo.Name);

                        await Navigation.PushAsync(page);
                    }
                    else if (openMode == FileOpenMode.ByImage)
                    {
                        ImageViewer page = (ImageViewer)Activator.CreateInstance(typeof(ImageViewer), ms, entryInfo.Name);

                        await Navigation.PushAsync(page);
                    }
                    else
                    {
                        await DisplayAlert("Information", "Please unzip the file and then view it.", "OK");
                        return;
                    }
                }
            }           
        }
    }

    private Stream ConvertToMemoryStream(Stream stream)
    {
        MemoryStream memoryStream = new MemoryStream();

        byte[] buffer = new byte[4096];

        StreamUtils.Copy(stream, memoryStream, buffer);

        memoryStream.Position = 0;

        return memoryStream;
    }

    private string GetDirectoryPath(string path)
    {
        int index = path.LastIndexOf("/");

        return path.Substring(0, index);
    }

    private void SetToolbarItemStatus(ToolbarItem item, bool enable)
    {
        FontImageSource fs = item.IconImageSource as FontImageSource;
        fs.Color = enable ? Colors.Blue : Colors.Transparent;

        item.IsEnabled = enable;
    }

    private void tbiBack_Clicked(object sender, EventArgs e)
    {
        string parentDirectory = this.GetDirectoryPath(this.currentEntryDirectory);

        if(!parentDirectory.Contains("/"))
        {
            this.SetToolbarItemStatus(this.tbiBack, false);
        }

        var infos = this.entryInfos.Where(item => this.GetDirectoryPath(item.Path) == parentDirectory);

        this.lvEntries.ItemsSource = infos;

        this.currentEntryDirectory = parentDirectory;
    }
}