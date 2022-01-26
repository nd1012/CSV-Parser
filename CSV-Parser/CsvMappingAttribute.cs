#if !NO_MAP
using System;

namespace wan24.Data
{
    /// <summary>
    /// CSV mapping 
    /// </summary>
    public class CsvMappingAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="field">Field index</param>
        public CsvMappingAttribute(int field) : base() => Field = field;

        /// <summary>
        /// Field index
        /// </summary>
        public int Field { get; }
    }
}
#endif
