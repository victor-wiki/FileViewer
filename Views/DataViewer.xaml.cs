using FileViewer.DataReader;
using FileViewer.Helper;
using FileViewer.Manager;
using FileViewer.Model;
using System.Text;
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
    private SettingInfo setting;

    public DataViewer(string filePath, FileOpenMode openMode)
    {
        InitializeComponent();

        this.filePath = filePath;
        this.fileOpenMode = openMode;

        this.lblTitle.Text = Path.GetFileName(filePath);

        this.Init();
    }

    public DataViewer(string filePath, FileOpenMode openMode, DatabaseObject databaseObject)
    {
        InitializeComponent();

        this.filePath = filePath;
        this.fileOpenMode = openMode;
        this.databaseObject = databaseObject;

        this.lblTitle.Text = databaseObject.Name;

        this.Init();
    }

    private async void Init()
    {
        this.setting = SettingManager.GetSetting();

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

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
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
            this.indicator.IsRunning = true;
            this.indicator.IsVisible = true;

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
            this.MainGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Absolute);
            this.indicator.IsRunning = false;
            this.indicator.IsVisible = false;
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

        await this.ShowGridData();
    }


    private async Task ShowGridData()
    {
        string dataArray = null;
        string strMergeCell = string.Empty;
        int columnWidth = 100;
        int rowHeight = 30;
        bool autoColumnSize = this.setting.AutoColumnSize;
        bool isMobile = DeviceInfo.Current.Platform == DevicePlatform.Android || DeviceInfo.Current.Platform == DevicePlatform.iOS;
        string strColumnWidth = autoColumnSize ? "undefined" : columnWidth.ToString();

        int columnCount = 0;

        if (this.columnHeaders != null)
        {
            columnCount = this.columnHeaders.Length;
        }
        else if (this.data != null && this.data.Count > 0)
        {
            columnCount = this.data.FirstOrDefault().Value.Keys.Count;
        }

        int tableWidth = columnCount * columnWidth + columnCount - 1;
        int tableHeight = (this.data.Count + (this.columnHeaders == null ? 0 : 1)) * rowHeight;

        if (this.data.Count > 0)
        {
            dataArray = DataHelper.ConvertDictionaryToArraryString(this.data);
        }
        else
        {
            dataArray = $"[['No data'{(columnCount > 1 ? $",{string.Join(",", Enumerable.Repeat("null", columnCount - 1))}" : "")}]]";

            strMergeCell =
 @",mergeCells: [ {row: 0, col: 0, rowspan: 1, colspan: " + columnCount + @"}],
  cell: [ { row: 0, col: 0, className: 'htCenter' }]";
        }

        string css = @"<link rel=""stylesheet"" href=""handsontable/handsontable.full.min.css"">";
        string js = @"<script type=""text/javascript""  src=""handsontable/handsontable.full.min.js""></script>";

        string columnHeaders = this.columnHeaders == null ? "false" : "[" + string.Join(",", this.columnHeaders.Select(item => $"'{item}'")) + "]";

        string html =
@"<html>
<head>
" + css + @"
" + js + @"
</head>
<body>  
  <input id='hidCellContent' type='hidden' />
  <div id='divControls' class=""controls"" style='display:none;margin-bottom:2px;'>
     <input id='txtKeyword' type='search' placeholder='Search'>
  </div>
  <div id='content' style='width:" + tableWidth + @"px;height:" + tableHeight + @"px;'/>
</body>
<script>

var isDataLoaded = false;
var isInited = false;
var hasSelection = false;
var isMobile = " + isMobile.ToString().ToLower() + @";
var tableDefaultWidth = " + tableWidth + @";
var tableDefaultHeight = " + tableHeight + @";
var wordWrap = " + (!isMobile).ToString().ToLower() + @";
var rowHeight=" + rowHeight + @";
var autoColumnSize = "+ autoColumnSize.ToString().ToLower() + @";

var container = document.getElementById('content');

var hot = new Handsontable(container, {
  readOnly:true,
  data: " + dataArray + @",
  rowHeaders: false,
  autoColumnSize: autoColumnSize,
  manualColumnResize:true, 
  colHeaders: " + columnHeaders + @",
  colWidths: " + strColumnWidth + @",
  maxRows: " + this.pageSize + @",
  wordWrap: wordWrap,
  rowHeights: rowHeight,  
  search: true, 
  afterSelectionEnd: function (row, col) {
      hasSelection=true;

      var content =hot.getDataAtCell(row, col);  

      if(content == null){
         content='';
      }

      document.getElementById('hidCellContent').value = content;
  },
  afterLoadData: function(firstLoad) {   
    isDataLoaded = true;
  },
  afterInit: function () {
     if(isInited == false && isMobile == true && autoColumnSize == true){
        setTableWidthByAutoColumnSize(this);
    }   

    isInited = true;
  },
  afterGetColHeader: function (col, TH) {
    TH.addEventListener('click', function () {
      hasSelection=true;
      var columnName = hot.getColHeader(col);
      document.getElementById('hidCellContent').value = columnName;
  });
  }" + strMergeCell + @"  
});

     function getTableTotalWidth(t){
        var totalWidth=0;
        var columnCount = t.countCols();
        for (var col = 0; col < columnCount ; col++) {
            totalWidth += t.getColWidth(col);
        }

        return totalWidth;
    };

    function setTableWidthByAutoColumnSize (t){
        var columnCount = t.countCols();
        container.style.width = (getTableTotalWidth(t) + columnCount -1) + 'px';
    };

    var txtKeyword =  document.getElementById('txtKeyword');

    txtKeyword.addEventListener('input', (event) => {  

        var search = hot.getPlugin('search');

        var queryResult = search.query(event.target.value);

        hot.render();
    });
</script>
<style>
:where(.ht-theme-main){
  --ht-background-secondary-color:#ffffff !important;
}

.hot-display-license-info {display: none !important;}
.handsontable .htDimmed { color: #000000; }
.handsontable td.htSearchResult {background-color:#fcedd9 !imporant; }
</style>
</html>";

        string viwer2Html =
@"<style>body {overflow: hidden;}</style>
<input id='txtCellContent' style='width:100%;'/>";

        this.viewer.Source = new HtmlWebViewSource { Html = html };
        this.viewer2.Source = new HtmlWebViewSource { Html = viwer2Html };
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

    private void tbiShowSearchControl_Clicked(object sender, EventArgs e)
    {
        string script =
@"var div=document.getElementById('divControls'); 
  if(div){ 
    var display=div.style.display;  
    div.style.display=display=='block'?'none':'block'; 
  }";

        this.ExecuteJavaScript(this.viewer, script);
    }

    private void tbiColumnSize_Clicked(object sender, EventArgs e)
    {
        string script =
@"if(isDataLoaded){
    var autoColumnSize = hot.getSettings().autoColumnSize;  

    hot.updateSettings({ autoColumnSize:autoColumnSize?false:true, colWidths:!autoColumnSize?undefined:100 });

    if(autoColumnSize){
      container.style.width=tableDefaultWidth + 'px';  
    }else{
      setTableWidthByAutoColumnSize(hot);
    }
}";

        this.ExecuteJavaScript(this.viewer, script);
    }

    private async void ExecuteJavaScript(WebView webView, string script)
    {
        if (webView.IsLoaded)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                var lines = script.Split(Environment.NewLine);

                foreach (var line in lines )
                {
                    if(line.TrimStart().StartsWith("//"))
                    {
                        continue;
                    }

                    sb.AppendLine(line);
                }

                string str = sb.ToString().Replace(Environment.NewLine, " ");

                await webView.EvaluateJavaScriptAsync(str);
            }
            catch (Exception ex)
            {

            }
        }
    }
}