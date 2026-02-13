using CsvHelper;
using CsvHelper.Configuration;
using FileViewer.Model;

namespace FileViewer.Helper
{
    public class CsvReadHelper
    {
        public static string ReadDataAsArrayString(string filePath)
        {
            var dataInfo = ReadData(filePath);

            return DataHelper.ConvertDictionaryToArraryString(dataInfo.Data);
        }

        public static DataInfo ReadData(string filePath, int? pageNumber = default(int?), int? pageSize = default(int?))
        {
            DataInfo dataInfo = new DataInfo();

            Dictionary<int, Dictionary<int, object>> dict = new Dictionary<int, Dictionary<int, object>>();

            using (StreamReader textReader = new StreamReader(filePath))
            {
                CsvConfiguration configuration = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture);

                configuration.Delimiter = ",";

                CsvReader reader = new CsvReader(textReader, configuration);

                int startRowIndex = 0;

                if (pageNumber.HasValue && pageSize.HasValue)
                {
                    var startEndRowNumber = PaginationHelper.GetStartEndRowNumber(pageNumber.Value, pageSize.Value);

                    startRowIndex = startEndRowNumber.StartRowNumber - 1;
                }

                int index = 0;
                int count = 0;
                int total = 0;

                while (reader.Read())
                {
                    total++;

                    if (index >= startRowIndex && count < pageSize.Value)
                    {
                        Dictionary<int, object> dictRow = new Dictionary<int, object>();

                        for (int i = 0; i < reader.ColumnCount; i++)
                        {
                            string value = reader.GetField(i);

                            dictRow.Add(i, value);
                        }

                        dict.Add(index, dictRow);
                        count++;
                    }
                   

                    index++;                   
                }

                dataInfo.Total = total;
                dataInfo.Data = dict;

                return dataInfo;
            }
        }
    }
}
