#if !NO_MAP
using System;

namespace wan24.Data
{
    /// <summary>
    /// CSV mapping
    /// </summary>
    public class CsvMapping : ICloneable
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
        public Func<string, object> ObjectValueFactory { get; set; }

        /// <summary>
        /// Row value factory
        /// </summary>
        public Func<object, string> RowValueFactory { get; set; }

        /// <summary>
        /// Field value pre-validation
        /// </summary>
        public Func<string, bool> PreValidation { get; set; }

        /// <summary>
        /// Field value post-validation
        /// </summary>
        public Func<object, bool> PostValidation { get; set; }

        /// <inheritdoc/>
        public object Clone() => new CsvMapping()
        {
            Field = Field,
            PropertyName = PropertyName,
            ObjectValueFactory = ObjectValueFactory,
            RowValueFactory = RowValueFactory,
            PreValidation = PreValidation,
            PostValidation = PostValidation
        };
    }
}
#endif
