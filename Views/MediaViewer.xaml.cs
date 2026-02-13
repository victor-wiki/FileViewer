namespace FileViewer.Views;

public partial class MediaViewer : ContentPage
{
	private string filePath;

	public MediaViewer(string filePath)
	{
		InitializeComponent();

		this.filePath = filePath;

		this.Title = Path.GetFileName(filePath);

		this.ShowMedia();
	}

	private void ShowMedia()
	{
		this.player.Source = this.filePath;
	}
}