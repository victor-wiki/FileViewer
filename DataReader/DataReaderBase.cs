using Dapper;
using FileViewer.Model;
using System.Data;
using System.Data.Common;
using Table = FileViewer.Model.Table;
using TableColumn = FileViewer.Model.TableColumn;
using View = FileViewer.Model.View;

namespace FileViewer.DataReader
{
    public abstract class DataReaderBase
    {
        protected string dataFilePath;

        public DataReaderBase(string filePath)
        {
            this.dataFilePath = filePath;
        }

        public abstract DbConnection CreateConnection();

        public abstract Task<List<Table>> GetTablesAsync();
        public abstract Task<List<Table>> GetTablesAsync(DbConnection dbConnection);

        public abstract Task<List<View>> GetViewsAsync();
        public abstract Task<List<View>> GetViewsAsync(DbConnection dbConnection);

        public abstract Task<List<TableColumn>> GetTableColumnsAsync(DatabaseObject tableOrView);
        public abstract Task<List<TableColumn>> GetTableColumnsAsync(DbConnection dbConnection, DatabaseObject tableOrView);

        public abstract Task<DataInfo> ReadData(DatabaseObject tableOrView, string orderColumns = null, string whereClause = null, int? pageNumber = default(int?), int? pageSize = default(int?));

        public async Task OpenConnectionAsync(DbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
        }        

        public virtual async Task<List<T>> GetDbObjectsAsync<T>(DbConnection dbConnection, string sql) where T : DatabaseObject
        {
            List<T> objects = new List<T>();

            if (!string.IsNullOrEmpty(sql))
            {
                await this.OpenConnectionAsync(dbConnection);

                objects = (await dbConnection.QueryAsync<T>(sql)).ToList();

                bool isAllOrdersIsZero = !objects.Any(item => item.Order != 0);

                if (isAllOrdersIsZero)
                {
                    int i = 1;

                    objects.ForEach(item =>
                    {
                        item.Order = i++;
                    });
                }
            }

            return objects;
        }

        public async Task<int> GetTableRecordCountAsync(DbConnection connection, DatabaseObject tableOrView, string whereClause = "")
        {
            return await this.GetRecordCount(tableOrView, connection, whereClause);
        }

        private Task<int> GetRecordCount(DatabaseObject dbObject, DbConnection connection, string whereClause = "")
        {
            string where = string.IsNullOrEmpty(whereClause) ? "" : $" {whereClause}";

            string sql = $"SELECT COUNT(1) FROM {dbObject.Name}{where}";

            return this.GetTableRecordCountAsync(connection, sql);
        }

        private async Task<int> GetTableRecordCountAsync(DbConnection connection, string sql)
        {
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public Dictionary<int, Dictionary<int, object>> ConvertDataTableToDictionary(DataTable dataTable, List<TableColumn> columns)
        {
            Dictionary<int, Dictionary<int, object>> rows = new Dictionary<int, Dictionary<int, object>>();

            int index = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                var dicField = new Dictionary<int, object>();

                for (var i = 0; i < dataTable.Columns.Count; i++)
                {
                    DataColumn column = dataTable.Columns[i];
                    string columnName = column.ColumnName;

                    TableColumn tableColumn = columns.FirstOrDefault(item => item.Name == columnName);

                    object value = row[i];

                    dicField.Add(i, value);
                }

                rows.Add(index, dicField);

                index++;
            }

            return rows;
        }
    }
}
