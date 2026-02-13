using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;
using System.Xml.Linq;

namespace FileViewer.Views;

public partial class WordViewer : ContentPage
{
    private string filePath;
    private Stream stream;

    public WordViewer(string filePath)
    {
        InitializeComponent();

        this.filePath = filePath;

        this.Title = Path.GetFileName(filePath);

        this.ShowWord();
    }

    public WordViewer(Stream stream, string title)
    {
        InitializeComponent();

        this.stream = stream;

        this.Title = title;

        this.ShowWord();
    }

    private async void ShowWord()
    {
        byte[] byteArray = null;

        if (!string.IsNullOrEmpty(this.filePath))
        {
            byteArray = File.ReadAllBytes(this.filePath);
        }
        else if (this.stream != null)
        {
            using(BinaryReader reader = new BinaryReader(this.stream))
            {
                byteArray = reader.ReadBytes((int)this.stream.Length);
            }           
        }

        if(byteArray == null)
        {
            return;
        }

        try
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(byteArray, 0, byteArray.Length);

                using (WordprocessingDocument doc = WordprocessingDocument.Open(memoryStream, true))
                {
                    HtmlConverterSettings settings = new HtmlConverterSettings();

                    XElement element = HtmlConverter.ConvertToHtml(doc, settings);

                    string html = element.ToStringNewLineOnAttributes();

                    this.viewer.Source = new HtmlWebViewSource { Html = html };
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}