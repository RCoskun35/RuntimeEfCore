using System.Text.RegularExpressions;

public static class SqlHelper
{
    public static bool IsValidColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return false;
        
        // SQL injection ve geçersiz karakterleri kontrol et
        string pattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";
        return Regex.IsMatch(columnName, pattern);
    }

    public static bool IsValidDataType(string dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType)) return false;

        // Geçerli SQL veri tiplerini kontrol et
        string[] validTypes = new[] { 
            "VARCHAR", "NVARCHAR", "CHAR", "NCHAR",
            "INT", "BIGINT", "SMALLINT", "TINYINT",
            "DECIMAL", "NUMERIC", "FLOAT", "REAL",
            "DATE", "DATETIME", "DATETIME2", "TIME",
            "BIT", "BINARY", "VARBINARY"
        };

        return validTypes.Any(t => dataType.ToUpper().StartsWith(t));
    }
} 