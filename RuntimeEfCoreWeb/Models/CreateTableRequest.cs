namespace RuntimeEfCoreWeb.Controllers
{
    public partial class DynamicTableController
    {
        public class CreateTableRequest
        {
            /// <summary>
            /// Name of the table to create
            /// </summary>
            public string TableName { get; set; }

            /// <summary>
            /// List of columns to create in the table
            /// </summary>
            public List<ColumnDefinition> Columns { get; set; }
        }
    }
}
