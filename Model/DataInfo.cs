namespace FileViewer.Model
{
    public class DataInfo
    {
        public int Total {  get; set; }
        public Dictionary<int, Dictionary<int, object>> Data { get; set; }
    }
}
