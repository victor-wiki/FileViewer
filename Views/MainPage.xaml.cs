using FileViewer.Helper;
using FileViewer.Model;
using FileViwer.Helper;

namespace FileViewer.Views
{
    public partial class MainPage : ContentPage
    {
       
        public MainPage()
        {
            InitializeComponent();
        }

        private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            if (!(await PermissionHelper.CheckReadWritePermission(PermissionType.Read)))
            {
                return;
            }

            var result = await FilePicker.Default.PickAsync();

            if (result != null && result.FullPath != null)
            {
                string filePath = result.FullPath;

                string extension = Path.GetExtension(filePath).ToLower();

                FileOpenMode openMode = FileHelper.GetFileOpenModeByExtension(extension);

                if(openMode == FileOpenMode.Unknown)
                {
                    openMode = FileHelper.DetectFileOpenMode(filePath);

                    if(openMode == FileOpenMode.Unknown)
                    {
                        await DisplayAlert("Information", "Not support.", "OK");
                        return;
                    }
                }

                this.indicator.IsRunning = true;
                this.openFileControl.IsVisible = false;

                if (openMode == FileOpenMode.ByImage)
                {
                    ImageViewer page = (ImageViewer)Activator.CreateInstance(typeof(ImageViewer), filePath);

                    await Navigation.PushAsync(page);
                }
                else if (openMode == FileOpenMode.ByMediaPath)
                {
                    MediaViewer page = (MediaViewer)Activator.CreateInstance(typeof(MediaViewer), filePath);

                    await Navigation.PushAsync(page);
                }
                else if (openMode == FileOpenMode.ByPdfPath)
                {
                    PdfViewer page = (PdfViewer)Activator.CreateInstance(typeof(PdfViewer), filePath);

                    await Navigation.PushAsync(page);
                }
                else if (openMode == FileOpenMode.ByWordParser)
                {
                    WordViewer page = (WordViewer)Activator.CreateInstance(typeof(WordViewer), filePath);

                    await Navigation.PushAsync(page);
                }
                else if (openMode == FileOpenMode.BySqlite || openMode == FileOpenMode.ByAccess)
                {
                    DbObjectList page = (DbObjectList)Activator.CreateInstance(typeof(DbObjectList), filePath, openMode);

                    await Navigation.PushAsync(page);
                }
                else if(openMode == FileOpenMode.ByTextContent)
                {
                    TextViewer page = (TextViewer)Activator.CreateInstance(typeof(TextViewer), filePath);

                    await Navigation.PushAsync(page);
                }
                else if (openMode == FileOpenMode.ByZip || openMode == FileOpenMode.ByRar)
                {
                    Explorer page = (Explorer)Activator.CreateInstance(typeof(Explorer), filePath);

                    await Navigation.PushAsync(page);
                }
                else
                {
                    DataViewer page = (DataViewer)Activator.CreateInstance(typeof(DataViewer), filePath, openMode);

                    await Navigation.PushAsync(page);
                }

                this.indicator.IsRunning = false;
                this.openFileControl.IsVisible = true;
            }
        }      
       
    }
}
