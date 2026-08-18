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
        ByVisioParser = 8,
        ByCsvParser = 9,
        BySqlite = 10,
        ByAccess = 11,
        ByZip = 12,
        ByRar = 13       
    }
}
