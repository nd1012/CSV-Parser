using System;

namespace wan24.Data
{
    /// <summary>
    /// CSV mapping
    /// </summary>
    public class CsvMapping
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CsvMapping() { }

        /// <summary>
        /// Field index
        /// </summary>
        public int Field { get; set; }

        /// <summary>
        /// Property name
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Object value factory
        /// </summary>
        public Func<string,object> ObjectValueFactory { get; set; }

        /// <summary>
        /// Row value factory
        /// </summary>
        public Func<object, string> RowValueFactory { get; set; }
    }
}
