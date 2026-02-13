using FileViewer.Core;
using FileViewer.Model;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using Table = FileViewer.Model.Table;
using TableColumn = FileViewer.Model.TableColumn;
using View = FileViewer.Model.View;

namespace FileViewer.DataReader
{
    public class AccessDataReader: DataReaderBase
    {
        public AccessDataReader(string filePath) : base(filePath) { }

        public override DbConnection CreateConnection()
        {
            string extension = Path.GetExtension(this.dataFilePath).ToLower();

            string connectionString = null;

            if (extension == ".mdb")
            {
                connectionString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={this.dataFilePath};";
            }
            else
            {
                connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={this.dataFilePath};";
            }

            OleDbConnection connection = new OleDbConnection(connectionString);  

            return connection;
        }

        public override Task<List<Table>> GetTablesAsync()
        {
            return this.GetTablesAsync(this.CreateConnection());
        }

        public override async Task<List<Table>> GetTablesAsync(DbConnection dbConnection)
        {
            await this.OpenConnectionAsync(dbConnection);

            var dt = await dbConnection.GetSchemaAsync("Tables");

            var tableNames = dt.AsEnumerable()
              .Where(row => row.Field<string>("TABLE_TYPE") == "TABLE")
              .Select(item=> item.Field<string>("TABLE_NAME"));

            return tableNames.Select(item => new Table() { Name = item }).ToList();
        }

        public override Task<List<View>> GetViewsAsync()
        {
            return this.GetViewsAsync(this.CreateConnection());
        }

        public override async Task<List<View>> GetViewsAsync(DbConnection dbConnection)
        {
            await this.OpenConnectionAsync(dbConnection);

            var dt = await dbConnection.GetSchemaAsync("Views");

            var tableNames = dt.AsEnumerable()
              .Where(row => row.Field<string>("TABLE_TYPE") == "VIEW")
              .Select(item => item.Field<string>("TABLE_NAME"));

            return tableNames.Select(item => new View() { Name = item }).ToList();
        }

        public override async Task<List<TableColumn>> GetTableColumnsAsync(DatabaseObject tableOrView)
        {
            return await this.GetTableColumnsAsync(this.CreateConnection(), tableOrView);
        }

        public override async Task<List<TableColumn>> GetTableColumnsAsync(DbConnection dbConnection, DatabaseObject tableOrView)
        {
            await this.OpenConnectionAsync(dbConnection);

            var dt = await dbConnection.GetSchemaAsync("Columns", [null, null, tableOrView.Name, null]);

            var columns = dt.AsEnumerable()
              .Select(item => new TableColumn() {
                  Name = item.Field<string>("COLUMN_NAME"),
                  TableName = item.Field<string>("TABLE_NAME")              
              }).ToList();

            return columns;
        }

        public override async Task<DataInfo> ReadData(DatabaseObject tableOrView, string orderColumns = null, string whereClause = null, int? pageNumber = default(int?), int? pageSize = default(int?))
        {
            DataInfo dataInfo = new DataInfo();

            using (DbConnection connection = this.CreateConnection())
            {
                dataInfo.Total = await this.GetTableRecordCountAsync(connection, tableOrView, whereClause);

                var columns = await this.GetTableColumnsAsync(connection, tableOrView);

                var columnNames = columns.Select(item => item.Name);

                string strColumnNames = string.Join(",", columnNames.Select(item=>$"[{item}]"));

                string orderByColumns = (!string.IsNullOrEmpty(orderColumns) ? orderColumns : string.Empty);

                string orderBy = !string.IsNullOrEmpty(orderByColumns) ? $" ORDER BY {orderByColumns}" : "";

                SqlBuilder sb = new SqlBuilder();

                sb.Append($@"SELECT {strColumnNames}
							  FROM {tableOrView.Name}
                             {whereClause} 
                             {orderBy}");

                string sql = sb.Content;

                DataTable table = new DataTable();

                int startRowIndex =pageNumber.HasValue? (pageNumber.Value - 1) * pageSize.Value:0;

                DbDataAdapter adapter = new OleDbDataAdapter(sql, connection as OleDbConnection);

                adapter.Fill(startRowIndex, pageSize.HasValue? pageSize.Value: dataInfo.Total, table);

                dataInfo.Data = this.ConvertDataTableToDictionary(table, columns);

                return dataInfo;
            }
        }      
    }
}
