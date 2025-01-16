namespace RuntimeEfCoreWeb.Controllers
{
    public partial class DynamicTableController
    {
        public class UpdateColumnRequest
        {
            /// <summary>
            /// Name of the table containing the column
            /// </summary>
            public string TableName { get; set; }

            /// <summary>
            /// Current name of the column to update
            /// </summary>
            public string OldColumnName { get; set; }

            /// <summary>
            /// New name for the column
            /// </summary>
            public string NewColumnName { get; set; }

            /// <summary>
            /// New data type for the column
            /// </summary>
            public string NewDataType { get; set; }
        }
    }
}
