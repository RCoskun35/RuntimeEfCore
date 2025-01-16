using Azure.Core;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RuntimeEfCoreWeb.Models;
using System.Linq.Dynamic.Core;
using static RuntimeEfCoreWeb.Controllers.DynamicTableController;

namespace RuntimeEfCoreWeb
{
    public class DynamicTableService
    {
        private readonly DbContext _context;

        public DynamicTableService()
        {
            _context = DynamicContextExtensions.dynamicContext;
        }

        public async Task<DynamicTable> CreateTableAsync(string tableName, List<DynamicColumn> columns)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var dynamicTable = new DynamicTable
                {
                    Name = tableName,
                    CreatedAt = DateTime.UtcNow,
                    Columns = columns
                };
                var createTableSql = GenerateCreateTableSql(tableName, columns);
                await _context.Database.ExecuteSqlRawAsync(createTableSql);

                await transaction.CommitAsync();
                return dynamicTable;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteTableAsync(string tableName)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync($"DROP TABLE [{tableName}]");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task AddColumnAsync(string tableName, DynamicColumn column)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var alterTableSql = $"ALTER TABLE [{tableName}] ADD [{column.Name}] {GetSqlDataType(column)}";
                await _context.Database.ExecuteSqlRawAsync(alterTableSql);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public string GenerateCreateTableSql(string tableName, List<DynamicColumn> columns)
        {
            var columnDefinitions = columns.Select(c => $"[{c.Name}] {GetSqlDataType(c)}");
            //
            return $"CREATE TABLE [{tableName}] (Id [uniqueidentifier] PRIMARY KEY, {string.Join(", ", columnDefinitions)})";
        }

        private string GetSqlDataType(DynamicColumn column)
        {
            var nullableStr = column.IsNullable ? "NULL" : "NOT NULL";
            return $"{column.DataType} {nullableStr}";
        }

        public async Task<List<string>> GetAllTables()
        {
            var tables = await _context.Database.SqlQuery<string>($@"
            SELECT TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE' 
            AND TABLE_NAME != '__EFMigrationsHistory'
            ORDER BY TABLE_NAME").ToListAsync();

            return tables;
        }
        public async Task<List<ColumnInfo>> GetTableColumns(string tableName)
        {
            var columns = await _context.Database.SqlQuery<ColumnInfo>($@"
            SELECT 
                COLUMN_NAME as ColumnName,
                DATA_TYPE + 
                CASE 
                    WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL 
                    THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')'
                    ELSE ''
                END as DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = {tableName}
            ORDER BY ORDINAL_POSITION").ToListAsync();

            return columns;
        }
        public async Task UpdateColumn(UpdateColumnRequest request)
        {
            string alterSql = $"ALTER TABLE {request.TableName} ALTER COLUMN {request.OldColumnName} {request.NewDataType}";
            await _context.Database.ExecuteSqlRawAsync(alterSql);

            // Eğer kolon adı değişiyorsa, kolon adını güncelle
            if (request.OldColumnName != request.NewColumnName)
            {
                string renameSql = $"EXEC sp_rename '{request.TableName}.{request.OldColumnName}', '{request.NewColumnName}', 'COLUMN'";
                await _context.Database.ExecuteSqlRawAsync(renameSql);
            }
        }
        public async Task DeleteColumn(string tableName, string columnName)
        {
            string sql = $"ALTER TABLE {tableName} DROP COLUMN {columnName}";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }
        public async Task Create(CreateTableRequest request)
        {
            var sql = new System.Text.StringBuilder();
            sql.AppendLine($"CREATE TABLE {request.TableName} (Id INT IDENTITY(1,1) PRIMARY KEY ,");

            var columnDefinitions = new List<string>();
            foreach (var column in request.Columns)
            {
                if (!SqlHelper.IsValidColumnName(column.Name) || !SqlHelper.IsValidDataType(column.DataType))
                {
                    throw new Exception($"Invalid column name or data type: {column.Name}");
                }

                var columnDef = $"{column.Name} {column.DataType}";
                if (column.IsPrimaryKey)
                {
                    columnDef += " PRIMARY KEY";
                    if (column.IsIdentity)
                    {
                        columnDef += " IDENTITY(1,1)";
                    }
                }
                else if (column.IsRequired)
                {
                    columnDef += " NOT NULL";
                }
                else
                {
                    columnDef += " NULL";
                }

                columnDefinitions.Add(columnDef);
            }

            sql.AppendLine(string.Join("," + Environment.NewLine, columnDefinitions));
            sql.AppendLine(")");
            // var aa = sql.ToString();
            await _context.Database.ExecuteSqlRawAsync(sql.ToString());
        }
        public async Task AddColumn(ColumnModel model)
        {
            string query = "";
            if (model.IsRequired)
            {
                query = $"ALTER TABLE {model.TableName} ADD {model.ColumnName} {model.ColumnType} NOT NULL DEFAULT  {GetDefault(model.ColumnType)}";
            }
            else
            {
                query = $"ALTER TABLE {model.TableName} ADD {model.ColumnName} {model.ColumnType} NULL";
            }
            await _context.Database.ExecuteSqlRawAsync(query);
        }
       
        private string GetDefault(string columnType)
        {
            if (columnType == "INT")
                return "0";
            else if (columnType == "VARCHAR(255)")
                return "''";
            else if (columnType == "DATETIME")
                return "GETDATE()";
            else if (columnType == "DECIMAL(18,2)")
                return "0.0";
            else if (columnType == "BIT")
                return "0";
            else
                return "''";
        }
        public async Task DeleteTable(string tableName)
        {
            string sql = $"DROP TABLE IF EXISTS {tableName}";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

    }
}
