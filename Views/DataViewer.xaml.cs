using FileViewer.DataReader;
using FileViewer.Helper;
using FileViewer.Model;
using SqliteDataReader = FileViewer.DAL.SqliteDataReader;

namespace FileViewer.Views;

public partial class DataViewer : ContentPage
{
    private string filePath;
    private FileOpenMode fileOpenMode;
    private DatabaseObject databaseObject;
    private ExcelInfo excelInfo;
    private int total;
    private Dictionary<int, Dictionary<int, object>> data;
    private int pageSize = 1000;
    private int pageCount;
    private int pageNumber = 1;
    private string[] columnHeaders;

    public DataViewer(string filePath, FileOpenMode openMode)
    {
        InitializeComponent();

        this.filePath = filePath;
        this.fileOpenMode = openMode;

        this.Title = Path.GetFileName(filePath);
    }

    public DataViewer(string filePath, FileOpenMode openMode, DatabaseObject databaseObject)
    {
        InitializeComponent();

        this.filePath = filePath;
        this.fileOpenMode = openMode;
        this.databaseObject = databaseObject;

        this.Title = databaseObject.Name;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        this.ShowContent();
    }

    private async void ShowContent()
    {
        try
        {
            this.viewer.IsVisible = false;
            this.indicator.IsRunning = true;

            switch (this.fileOpenMode)
            {
                case FileOpenMode.ByExcelParser:
                    this.ShowExcel();
                    break;
                case FileOpenMode.ByCsvParser:
                    this.ShowCsv(1);
                    break;
                case FileOpenMode.BySqlite:
                case FileOpenMode.ByAccess:
                    this.ShowTableData(1);
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            this.indicator.IsRunning = false;
            this.viewer.IsVisible = true;
        }
    }

    private void ShowExcel()
    {
        var info = ExcelHelper.GetInfo(this.filePath);

        this.excelInfo = info;

        int sheetCount = info.SheetCount;

        if (sheetCount > 0)
        {
            this.picker.IsVisible = info.SheetCount > 1;

            if (sheetCount > 1)
            {
                info.Sheets.ForEach(item => this.picker.Items.Add(item.Name));

                this.picker.SelectedIndex = 0;
            }
            else
            {
                this.ShowExcelSheetData(0, 1);
            }
        }
    }

    private void ShowExcelSheetData(int sheetIndex, int pageNumber)
    {
        this.data = ExcelHelper.ReadSheetData(this.filePath, sheetIndex, pageNumber, this.pageSize);

        this.total = this.excelInfo.Sheets[sheetIndex].RowsCount;

        this.ShowData(pageNumber);
    }

    private void ShowCsv(int pageNumber)
    {
        DataInfo dataInfo = CsvReadHelper.ReadData(this.filePath, pageNumber, this.pageSize);

        this.total = dataInfo.Total;
        this.data = dataInfo.Data;

        this.ShowData(1);
    }

    private async void ShowTableData(int pageNumber)
    {
        DataReaderBase reader = null;

        if (this.fileOpenMode == FileOpenMode.BySqlite)
        {
            reader = new SqliteDataReader(this.filePath);
        }
        else if (this.fileOpenMode == FileOpenMode.ByAccess)
        {
            reader = new AccessDataReader(this.filePath);
        }

        if (reader != null)
        {
            DataInfo dataInfo = await reader.ReadData(this.databaseObject, null, null, pageNumber, this.pageSize);

            this.columnHeaders = (await reader.GetTableColumnsAsync(this.databaseObject)).Select(item => item.Name).ToArray();

            this.total = dataInfo.Total;
            this.data = dataInfo.Data;

            this.ShowData(1);
        }
    }


    private void ShowData(int pageNumber)
    {
        this.pageCount = this.total % this.pageSize == 0 ? this.total / this.pageSize : this.total / this.pageSize + 1;

        this.toolbarGrid.IsVisible = this.pageCount > 1;

        this.pagination.IsVisible = this.pageCount > 1;

        this.btnFirst.IsEnabled = pageNumber != 1;
        this.btnPrevious.IsEnabled = this.pageNumber > 1;
        this.btnNext.IsEnabled = this.pageNumber < this.pageCount;
        this.btnLast.IsEnabled = pageNumber != this.pageCount;

        string dataArray = DataHelper.ConvertDictionaryToArraryString(this.data);

        this.ShowGridData(dataArray);
    }


    private async void ShowGridData(string data)
    {
        string css = await this.ReadResourceFileContent("handsontable/handsontable.min.css");
        string js = await this.ReadResourceFileContent("handsontable/handsontable.min.js");

        string columnHeaders = this.columnHeaders == null ? "false" : "[" + string.Join(",", this.columnHeaders.Select(item => $"'{item}'")) + "]";

        string html =
@"<html>
<head>
  <style>
" + css + @"
</style>
<script>
" + js + @"
</script>
</head>
<body>
 <div id='content'/>
</body>
<script>

var container = document.getElementById('content');
var hot = new Handsontable(container, {
  readOnly:true,
  data: " + data + @",
  rowHeaders: false,
  manualColumnResize:true,
  colHeaders: " + columnHeaders + @",
  colWidths: 100,
  maxRows: " + this.pageSize + @"
})
</script>
<style>
.hot-display-license-info {display: none !important;}
.handsontable .htDimmed { color: #000000; }
</style>
</html>";

        this.viewer.Source = new HtmlWebViewSource { Html = html };
    }

    private async Task<string> ReadResourceFileContent(string path)
    {
        using (StreamReader sr = new StreamReader(await FileSystem.Current.OpenAppPackageFileAsync(path)))
        {
            return await sr.ReadToEndAsync();
        }
    }

    private void ShowPagedData(int pageNumber)
    {
        switch (this.fileOpenMode)
        {
            case FileOpenMode.ByExcelParser:
                this.ShowExcelSheetData(this.picker.SelectedIndex, pageNumber);
                break;
            case FileOpenMode.ByCsvParser:
                this.ShowCsv(pageNumber);
                break;
            case FileOpenMode.BySqlite:
            case FileOpenMode.ByAccess:
                this.ShowTableData(pageNumber);
                break;
        }
    }

    private void btnFirst_Clicked(object sender, EventArgs e)
    {
        this.pageNumber = 1;

        this.ShowPagedData(this.pageNumber);
    }

    private void btnPrevious_Clicked(object sender, EventArgs e)
    {
        this.pageNumber--;

        this.ShowPagedData(this.pageNumber);
    }

    private void btnNext_Clicked(object sender, EventArgs e)
    {
        this.pageNumber++;

        this.ShowPagedData(this.pageNumber);
    }


    private void btnLast_Clicked(object sender, EventArgs e)
    {
        this.pageNumber = this.pageCount;

        this.ShowPagedData(this.pageNumber);
    }

    private void picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        int index = this.picker.SelectedIndex;

        this.ShowExcelSheetData(index, this.pageNumber);
    }
}