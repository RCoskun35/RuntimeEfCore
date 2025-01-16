using RuntimeEfCoreWeb.Controllers;

namespace RuntimeEfCoreWeb.Models
{
    public class DynamicColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public int DynamicTableId { get; set; }
        public DynamicTable DynamicTable { get; set; }
    }
    public class DynamicTable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<DynamicColumn> Columns { get; set; }
    }
}
