using System.Text;

namespace FileViewer.Helper
{
    public class DataHelper
    {
        public static string ConvertDictionaryToArraryString(Dictionary<int, Dictionary<int, object>> dict)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[");

            int i = 0;

            foreach (var kp in dict)
            {
                var values = kp.Value;

                sb.AppendLine($"[{string.Join(",", values.Select(item => GetCellValue(item.Value)))}]{(i == dict.Count - 1 ? "" : ",")}");

                i++;               
            }

            sb.AppendLine("]");

            return sb.ToString();
        }

        private static string GetCellValue(object value)
        {
            if(value == null)
            {
                return string.Empty;
            }

            string strValue = value.ToString();

            if(int.TryParse(strValue, out _))
            {
                return strValue;
            }
            else
            {
                return $"'{strValue.Replace("'","\\'").Replace("\n","\\n").Replace("\r", "\\r")}'";
            }
        }
    }
}
