namespace FileViewer.Views;

public partial class FileList : ContentPage
{
    public FileList(string folder)
    {
        InitializeComponent();

        this.ShowFiles(folder);
    }

    private void ShowFiles(string folder)
    {
        var files = new DirectoryInfo(folder).GetFiles().OrderByDescending(item => item.CreationTime);

        this.lvFile.ItemsSource = files;
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Border border = sender as Border;

        FileInfo fileInfo = border.BindingContext as FileInfo;

        if (fileInfo != null)
        {
            TextViewer textViewer = (TextViewer)Activator.CreateInstance(typeof(TextViewer), fileInfo.FullName);

            await Navigation.PushAsync(textViewer);
        }
    }
}