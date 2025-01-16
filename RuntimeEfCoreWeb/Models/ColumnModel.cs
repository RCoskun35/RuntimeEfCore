namespace RuntimeEfCoreWeb.Models
{
    public class ColumnModel
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public bool IsRequired { get; set; }
    }
}
