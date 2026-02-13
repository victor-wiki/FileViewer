namespace FileViewer.Model
{
    public class DatabaseObject
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
    }

    [Flags]
    public enum DatabaseObjectType : int
    {
        None = 0,
        Table = 2,
        View = 4      
    }
}
