namespace FileViewer.Views;

public partial class PdfViewer : ContentPage
{
    private string filePath;
    public PdfViewer(string filePath)
	{
		InitializeComponent();

		this.filePath = filePath;

        this.Title = Path.GetFileName(filePath);

        this.ShowPdf();
	}

	private void ShowPdf()
	{
		this.pdf.Uri = this.filePath;		
	}
}