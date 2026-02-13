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
        ByExcelParser = 6,
        ByCsvParser = 7,
        BySqlite = 8,
        ByAccess = 9,
        ByZip = 10,
        ByRar = 11
    }
}
