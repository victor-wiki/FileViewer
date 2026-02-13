namespace FileViewer.Model
{
    public class ExcelInfo
    {
        public List<SheetInfo> Sheets { get; set; }

        public int SheetCount => this.Sheets != null ? this.Sheets.Count : 0;
    }

    public class SheetInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool IsHidden { get; set; }
        public int RowsCount { get; set; }
    }
}
