using Dapper;
using FileViewer.Core;
using FileViewer.DataReader;
using FileViewer.Helper;
using FileViewer.Model;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;
using Table = FileViewer.Model.Table;
using TableColumn = FileViewer.Model.TableColumn;
using View = FileViewer.Model.View;

namespace FileViewer.DAL
{
    public class SqliteDataReader:DataReaderBase
    {
        public SqliteDataReader(string filePath) : base(filePath) { }       

        public override DbConnection CreateConnection()
        {
            DbProviderFactory factory = SqliteFactory.Instance;

            DbConnection connection = factory.CreateConnection();

            connection.ConnectionString = $"Data Source={this.dataFilePath}";

            return connection;
        }       

        #region Table  

        public override Task<List<Table>> GetTablesAsync()
        {
            return this.GetDbObjectsAsync<Table>(this.CreateConnection(), this.GetSqlForTables());
        }

        public override Task<List<Table>> GetTablesAsync(DbConnection dbConnection)
        {
            return this.GetDbObjectsAsync<Table>(dbConnection, this.GetSqlForTables());
        }

        private string GetSqlForTables()
        {
            return this.GetSqlForTableViews(DatabaseObjectType.Table);
        }

        private string GetSqlForTableViews(DatabaseObjectType dbObjectType)
        {
            SqlBuilder sb = new SqlBuilder();

            string type = dbObjectType.ToString().ToLower();
          
            sb.Append($@"SELECT name AS Name FROM sqlite_schema WHERE type= '{type}' AND name not in('sqlite_sequence')");

            sb.Append("ORDER BY name");

            return sb.Content;
        }
        #endregion

        #region View    

        public override Task<List<View>> GetViewsAsync()
        {
            return this.GetDbObjectsAsync<View>(this.CreateConnection(), this.GetSqlForViews());
        }

        public override Task<List<View>> GetViewsAsync(DbConnection dbConnection)
        {
            return this.GetDbObjectsAsync<View>(dbConnection, this.GetSqlForViews());
        }

        private string GetSqlForViews()
        {
            return this.GetSqlForTableViews(DatabaseObjectType.View);
        }
        #endregion

        #region Table Columns

        public override async Task<List<TableColumn>> GetTableColumnsAsync(DatabaseObject tableOrView)
        {
            var columns = await this.GetDbObjectsAsync<TableColumn>(this.CreateConnection(), this.GetSqlForTableColumns(tableOrView));

            return columns;
        }

        public override async Task<List<TableColumn>> GetTableColumnsAsync(DbConnection dbConnection, DatabaseObject tableOrView)
        {
            var columns = await this.GetDbObjectsAsync<TableColumn>(dbConnection, this.GetSqlForTableColumns(tableOrView));

            return columns;
        }

        private string GetSqlForTableColumns(DatabaseObject tableOrView)
        {
            string tableName = tableOrView.Name;

            SqlBuilder sb = new SqlBuilder();

            sb.Append($@"SELECT name AS Name,'{tableName}' AS TableName,
                                type AS DataType,
                                CASE WHEN INSTR(UPPER(type),'NUMERIC')>=1 AND INSTR(type,'(')>0 THEN CAST(TRIM(SUBSTR(type,INSTR(type,'(')+1,IIF(INSTR(type,',')==0, INSTR(type,')'),INSTR(type,','))-INSTR(type,'(')-1)) AS INTEGER) ELSE NULL END AS Precision,
                                CASE WHEN INSTR(UPPER(type),'NUMERIC')>=1 AND INSTR(type,',')>0 THEN CAST(TRIM(SUBSTR(type,INSTR(type,',')+1,INSTR(type,')')-INSTR(type,',')-1)) AS INTEGER) ELSE NULL END AS Scale,
                                CASE WHEN type='INTEGER' AND pk=1 AND EXISTS( SELECT 1 FROM sqlite_master WHERE  name = '{tableName}' AND sql LIKE '%AUTOINCREMENT%') THEN 1 ELSE 0 END AS IsIdentity,
                                CASE WHEN ""notnull""=1 THEN 0 ELSE 1 END AS IsNullable,
                                dflt_value AS DefaultValue, pk AS IsPrimaryKey, cid AS ""Order""
                                FROM PRAGMA_TABLE_XINFO('{tableName}')");

            return sb.Content;
        }
        #endregion

        public override async Task<DataInfo> ReadData(DatabaseObject tableOrView, string orderColumns = null, string whereClause = null, int? pageNumber = default(int?), int? pageSize = default(int?))
        {
            DataInfo dataInfo = new DataInfo();

            using (DbConnection connection = this.CreateConnection())
            {
                dataInfo.Total = await this.GetTableRecordCountAsync(connection, tableOrView, whereClause);

                var columns = await this.GetTableColumnsAsync(connection, tableOrView);

                var columnNames = columns.Select(item => item.Name);

                string strColumnNames = string.Join(",", columnNames);

                string pagedSql = this.GetSqlForPagination(tableOrView.Name, strColumnNames, orderColumns, whereClause, pageNumber, pageSize);

                var cmd = connection.CreateCommand();

                cmd.CommandText = pagedSql;

                DbDataReader reader = await cmd.ExecuteReaderAsync();

                DataTable table = new DataTable();

                table.Load(reader);

                dataInfo.Data = this.ConvertDataTableToDictionary(table, columns);

                return dataInfo;
            }
        }       

        public string GetSqlForPagination(string tableName, string columnNames, string orderColumns, string whereClause, int? pageNumber=default(int?), int? pageSize=default(int?))
        {
            string orderByColumns = (!string.IsNullOrEmpty(orderColumns) ? orderColumns : string.Empty);

            string orderBy = !string.IsNullOrEmpty(orderByColumns) ? $" ORDER BY {orderByColumns}" : "";

            SqlBuilder sb = new SqlBuilder();

            sb.Append($@"SELECT {columnNames}
							  FROM {tableName}
                             {whereClause} 
                             {orderBy}");

            if(pageNumber.HasValue && pageSize.HasValue)
            {
                var startEndRowNumber = PaginationHelper.GetStartEndRowNumber(pageNumber.Value, pageSize.Value);

                sb.Append($"LIMIT {pageSize} OFFSET {startEndRowNumber.StartRowNumber - 1}");
            }    

            return sb.Content;
        }        
    }
}
