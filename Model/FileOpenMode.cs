namespace FileViewer.Model
{
    public enum FileOpenMode
    {
        Unknown = 0,
        ByImage = 1,
        ByMediaPath = 2,
        ByPdfPath = 3,
        ByTextContent = 4,
        ByWordParser = 5,
        ByPowerPointParser = 6,
        ByExcelParser = 7,
        ByCsvParser = 8,
        BySqlite = 9,
        ByAccess = 10,
        ByZip = 11,
        ByRar = 12
    }
}
