#if !NO_MAP
using System;
using System.Collections.Generic;
using System.Linq;

namespace wan24.Data
{
    /// <summary>
    /// CSV mappings
    /// </summary>
    public class CsvMappings : Dictionary<int, CsvMapping>, ICloneable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CsvMappings() : base() { }

        /// <summary>
        /// Get a mapping
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Mapping or <see langword="null"/></returns>
        public CsvMapping this[string propertyName] => (from mapping in Values where mapping.PropertyName == propertyName select mapping).FirstOrDefault();

        /// <inheritdoc/>
        public object Clone()
        {
            CsvMappings res = new CsvMappings();
            foreach (CsvMapping map in Values) res[map.Field] = map.Clone() as CsvMapping;
            return res;
        }
    }
}
#endif
