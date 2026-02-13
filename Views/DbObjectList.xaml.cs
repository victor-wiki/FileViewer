using FileViewer.DAL;
using FileViewer.DataReader;
using FileViewer.Model;
using System.Data.Common;

namespace FileViewer.Views;

public partial class DbObjectList : ContentPage
{
    private string filePath;
    private FileOpenMode fileOpenMode;

    public DbObjectList(string filePath, FileOpenMode fileOpenMode)
	{
		InitializeComponent();

        this.filePath = filePath;
        this.fileOpenMode = fileOpenMode;

        this.picker.Items.Add("Tables");
        this.picker.Items.Add("Views");

        this.picker.SelectedIndex = 0;
    }

    private async void ShowDbObjects()
    {
        int index = this.picker.SelectedIndex;



        DataReaderBase reader = null;
        
        if(this.fileOpenMode == FileOpenMode.BySqlite)
        {
            reader = new SqliteDataReader(this.filePath);
        }
        else if(this.fileOpenMode == FileOpenMode.ByAccess)
        {
            reader = new AccessDataReader(this.filePath);
        }

        if(reader!=null)
        {
            using (DbConnection connection = reader.CreateConnection())
            {
                IEnumerable<DatabaseObject> dbObjects = null;

                if (index == 0)
                {
                    dbObjects = await reader.GetTablesAsync();
                }
                else
                {
                    dbObjects = await reader.GetViewsAsync();
                }

                this.lvObjects.ItemsSource = dbObjects;
            }
        }            
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Grid grid = sender as Grid;

        var dbObject = grid.BindingContext as DatabaseObject;

        if (dbObject != null)
        {
            DataViewer page = (DataViewer)Activator.CreateInstance(typeof(DataViewer), this.filePath, this.fileOpenMode, dbObject);

            await Navigation.PushAsync(page);
        }
    }

    private void picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        this.ShowDbObjects();
    }
}