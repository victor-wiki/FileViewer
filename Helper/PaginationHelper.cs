namespace FileViewer.Helper
{
    public class PaginationHelper
    {
        public static int GetPageCount(int total, int pageSize)
        {
            return total % pageSize == 0 ? total / pageSize : total / pageSize + 1;
        }

        public static (int StartRowNumber, int EndRowNumber) GetStartEndRowNumber(int pageNumber, int pageSize)
        {
            int startRowNumber = (pageNumber - 1) * pageSize + 1;
            int endRowNumber = pageNumber * pageSize;

            return (startRowNumber, endRowNumber);
        }
    }
}
