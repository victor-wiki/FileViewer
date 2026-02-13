namespace FileViewer.Views;

public partial class ImageViewer : ContentPage
{
    private string filePath;
    private Stream stream;

    public ImageViewer(string filePath)
	{
		InitializeComponent();

        this.filePath = filePath;

        this.Title = Path.GetFileName(filePath);

        this.ShowImage();
    }

    public ImageViewer(Stream stream, string title)
    {
        InitializeComponent();

        this.stream = stream;

        this.Title = title;

        this.ShowImage();
    }

    private void ShowImage()
    {
        if (!string.IsNullOrEmpty(this.filePath))
        {
            FileStream fs = File.OpenRead(this.filePath);

            var len=  fs.Length;

            this.img.Source = this.filePath;
        }
        else if(this.stream!=null)
        {
            this.img.Source = ImageSource.FromStream(()=> this.stream);
        }
    }
}