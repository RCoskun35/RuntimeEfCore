namespace RuntimeEfCoreWeb.Models
{
        public class ColumnDefinition
        {
            /// <summary>
            /// Name of the column
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// SQL data type of the column (e.g., VARCHAR(255), INT, etc.)
            /// </summary>
            public string DataType { get; set; }

            /// <summary>
            /// Whether the column is required (NOT NULL)
            /// </summary>
            public bool IsRequired { get; set; }

            /// <summary>
            /// Whether the column is a primary key
            /// </summary>
            public bool IsPrimaryKey { get; set; }

            /// <summary>
            /// Whether the column is an identity column (auto-increment)
            /// </summary>
            public bool IsIdentity { get; set; }
        }
    }

