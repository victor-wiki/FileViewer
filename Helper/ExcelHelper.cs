using FileViewer.Model;
using NPOI.SS.UserModel;

namespace FileViewer.Helper
{
    public class ExcelHelper
    {
        public static ExcelInfo GetInfo(string filePath)
        {
            ExcelInfo info = new ExcelInfo();

            Dictionary<int, Dictionary<int, object>> dict = new Dictionary<int, Dictionary<int, object>>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = WorkbookFactory.Create(fs);

                int sheetCount = workbook.NumberOfSheets;

                if (sheetCount > 0)
                {
                    info.Sheets = new List<SheetInfo>();
                }

                for (int i = 0; i < sheetCount; i++)
                {
                    ISheet sheet = workbook.GetSheetAt(i);

                    SheetInfo sheetInfo = new SheetInfo();
                    sheetInfo.Index = i;
                    sheetInfo.Name = sheet.SheetName;
                    sheetInfo.IsHidden = workbook.IsSheetHidden(i);
                    sheetInfo.RowsCount = sheet.LastRowNum - sheet.FirstRowNum + 1;

                    info.Sheets.Add(sheetInfo);
                }
            }

            return info;
        }

        public static string ReadSheetDataAsArrayString(string filePath, int sheetIndex = 0)
        {
            var dict = ReadSheetData(filePath, sheetIndex);

            return DataHelper.ConvertDictionaryToArraryString(dict);
        }

        public static Dictionary<int, Dictionary<int, object>> ReadSheetData(string filePath, int sheetIndex = 0, int? pageNumber = default(int?), int? pageSize = default(int?))
        {
            Dictionary<int, Dictionary<int, object>> dict = new Dictionary<int, Dictionary<int, object>>();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = WorkbookFactory.Create(fs);

                int sheetCount = workbook.NumberOfSheets;

                ISheet sheet = workbook.GetSheetAt(sheetIndex);

                int startRowIndex = sheet.FirstRowNum;
                int endRowIndex = sheet.LastRowNum;

                if (pageNumber.HasValue && pageSize.HasValue)
                {
                    var startEndRowNumber = PaginationHelper.GetStartEndRowNumber(pageNumber.Value, pageSize.Value);

                    startRowIndex = startEndRowNumber.StartRowNumber - 1;
                    endRowIndex = startEndRowNumber.EndRowNumber - 1 > endRowIndex ? endRowIndex : startEndRowNumber.EndRowNumber - 1;
                }

                for (int j = startRowIndex; j <= endRowIndex; j++)
                {
                    var row = sheet.GetRow(j);

                    int k = 0;
                    int cellCount = row.LastCellNum;

                    Dictionary<int, object> dictRow = new Dictionary<int, object>();

                    for (int m = 0; m < cellCount; m++)
                    {
                        var cell = row.GetCell(m);

                        dictRow.Add(k, GetCellValue(cell));

                        k++;
                    }

                    if (dictRow.Any())
                    {
                        dict.Add(j, dictRow);
                    }
                }
            }

            return dict;
        }

        public static object GetCellValue(ICell cell)
        {
            if (cell == null)
            {
                return null;
            }

            switch (cell.CellType)
            {
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Numeric:
                    return cell.NumericCellValue;
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Error:
                case CellType.Blank:
                case CellType.Formula:
                    return null;
                default:
                    return cell.StringCellValue;
            }
        }
    }
}
