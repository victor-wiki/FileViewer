namespace FileViewer.Views;

public partial class TextViewer : ContentPage
{
    private string filePath;
    private Stream stream;

    public TextViewer(string filePath)
    {
        InitializeComponent();

        this.filePath = filePath;

        this.Title = Path.GetFileName(filePath);

        this.ShowText();
    }

    public TextViewer(Stream stream, string title)
    {
        InitializeComponent();

        this.stream = stream;

        this.Title = title;

        this.ShowText();
    }

    private void ShowText()
    {
        if (!string.IsNullOrEmpty(this.filePath))
        {
            this.txtContent.Text = File.ReadAllText(this.filePath);
        }
        else if (this.stream != null)
        {
            using (StreamReader reader = new StreamReader(this.stream))
            {
                string content = reader.ReadToEnd();

                this.txtContent.Text = content;
            }
        }
    }
}