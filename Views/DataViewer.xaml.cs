using FileViewer.DataReader;
using FileViewer.Helper;
using FileViewer.Model;
using System.Threading.Tasks;
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
    private IDispatcherTimer timer;

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

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        this.timer = Dispatcher.CreateTimer();
        this.timer.Interval = TimeSpan.FromMilliseconds(500);
        this.timer.Tick += this.Timer_Tick;

        await this.ShowContent();

        this.timer.Start();
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        if (this.viewer.IsLoaded)
        {
            try
            {
                if (await this.HasSelection())
                {
                    string content = await this.viewer.EvaluateJavaScriptAsync("document.getElementById('hidCellContent').value;");

                    if (content != "null")
                    {
                        var cleanContent = content.Replace("'", "\\'").Replace("\\n", " ").Replace("\\r", " ");

                        await this.viewer2.EvaluateJavaScriptAsync($"var txt=document.getElementById('txtCellContent'); if(txt) txt.value='{cleanContent}';");
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }

    private async Task<bool> HasSelection()
    {
        try
        {
            var hasSelection = await this.viewer.EvaluateJavaScriptAsync("hasSelection");

            return hasSelection == "true";
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        if (this.timer != null)
        {
            this.timer.Stop();
        }
    }

    private async Task ShowContent()
    {
        try
        {
            this.viewer.IsVisible = false;
            this.indicator.IsRunning = true;

            switch (this.fileOpenMode)
            {
                case FileOpenMode.ByExcelParser:
                    await this.ShowExcel();
                    break;
                case FileOpenMode.ByCsvParser:
                    await this.ShowCsv(1);
                    break;
                case FileOpenMode.BySqlite:
                case FileOpenMode.ByAccess:
                    await this.ShowTableData(1);
                    break;
            }

            this.toolbarGrid.IsVisible = this.picker.IsVisible || this.pagination.IsVisible;

            if (!this.picker.IsVisible && this.pagination.IsVisible)
            {
                this.toolbarGrid.SetColumn(this.pagination, 0);
                this.toolbarGrid.SetColumnSpan(this.pagination, 2);
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

    private async Task ShowExcel()
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
                await this.ShowExcelSheetData(0, 1);
            }
        }
    }

    private async Task ShowExcelSheetData(int sheetIndex, int pageNumber)
    {
        this.data = ExcelHelper.ReadSheetData(this.filePath, sheetIndex, pageNumber, this.pageSize);

        this.total = this.excelInfo.Sheets[sheetIndex].RowsCount;

        await this.ShowData(pageNumber);
    }

    private async Task ShowCsv(int pageNumber)
    {
        DataInfo dataInfo = CsvReadHelper.ReadData(this.filePath, pageNumber, this.pageSize);

        this.total = dataInfo.Total;
        this.data = dataInfo.Data;

        await this.ShowData(pageNumber);
    }

    private async Task ShowTableData(int pageNumber)
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

            await this.ShowData(pageNumber);
        }
    }


    private async Task ShowData(int pageNumber)
    {
        this.pageCount = this.total % this.pageSize == 0 ? this.total / this.pageSize : this.total / this.pageSize + 1;

        this.pagination.IsVisible = this.pageCount > 1;

        this.toolbarGrid.IsVisible = this.picker.IsVisible || this.pagination.IsVisible;

        this.btnFirst.IsEnabled = pageNumber != 1;
        this.btnPrevious.IsEnabled = this.pageNumber > 1;
        this.btnNext.IsEnabled = this.pageNumber < this.pageCount;
        this.btnLast.IsEnabled = pageNumber != this.pageCount;

        if (DeviceInfo.Current.Platform == DevicePlatform.WinUI || !this.picker.IsVisible)
        {
            this.lblPageInfo.IsVisible = true;
            this.lblPageInfo.Text = $"({pageNumber}/{this.pageCount})";
        }

        string dataArray = DataHelper.ConvertDictionaryToArraryString(this.data);

        await this.ShowGridData(dataArray);
    }


    private async Task ShowGridData(string data)
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
  <input id='hidCellContent' type='hidden' />
  <div id='content'/>
</body>
<script>
var hasSelection=false;

var container = document.getElementById('content');
var hot = new Handsontable(container, {
  readOnly:true,
  data: " + data + @",
  rowHeaders: false,
  manualColumnResize:true,
  colHeaders: " + columnHeaders + @",
  colWidths: 100,
  maxRows: " + this.pageSize + @",
  search: true,
  afterSelectionEnd: function (row, col) {
      hasSelection=true;

      var content =hot.getDataAtCell(row, col);  

      if(content == null){
         content='';
      }

      document.getElementById('hidCellContent').value = content;
  }  
});

</script>
<style>
.hot-display-license-info {display: none !important;}
.handsontable .htDimmed { color: #000000; }
</style>
</html>";

        string viwer2Html =
@"<style>body {overflow: hidden;}</style>
<input id='txtCellContent' style='width:100%;'/>";

        this.viewer.Source = new HtmlWebViewSource { Html = html };
        this.viewer2.Source = new HtmlWebViewSource { Html = viwer2Html };
    }

    private async Task<string> ReadResourceFileContent(string path)
    {
        using (StreamReader sr = new StreamReader(await FileSystem.Current.OpenAppPackageFileAsync(path)))
        {
            return await sr.ReadToEndAsync();
        }
    }

    private async Task ShowPagedData(int pageNumber)
    {
        switch (this.fileOpenMode)
        {
            case FileOpenMode.ByExcelParser:
                await this.ShowExcelSheetData((this.picker.SelectedIndex == -1 ? 0 : this.picker.SelectedIndex), pageNumber);
                break;
            case FileOpenMode.ByCsvParser:
                await this.ShowCsv(pageNumber);
                break;
            case FileOpenMode.BySqlite:
            case FileOpenMode.ByAccess:
                await this.ShowTableData(pageNumber);
                break;
        }
    }

    private async void btnFirst_Clicked(object sender, EventArgs e)
    {
        this.pageNumber = 1;

        await this.ShowPagedData(this.pageNumber);
    }

    private async void btnPrevious_Clicked(object sender, EventArgs e)
    {
        this.pageNumber--;

        await this.ShowPagedData(this.pageNumber);
    }

    private async void btnNext_Clicked(object sender, EventArgs e)
    {
        this.pageNumber++;

        await this.ShowPagedData(this.pageNumber);
    }


    private async void btnLast_Clicked(object sender, EventArgs e)
    {
        this.pageNumber = this.pageCount;

        await this.ShowPagedData(this.pageNumber);
    }

    private async void picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        int index = this.picker.SelectedIndex;

        this.pageNumber = 1;

        await this.ShowExcelSheetData(index, this.pageNumber);
    }
}