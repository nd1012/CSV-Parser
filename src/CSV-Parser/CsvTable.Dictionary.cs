#if !NO_DICT
using System;
using System.Collections.Generic;
using System.Linq;

namespace wan24.Data
{
    public partial class CsvTable
    {
        /// <summary>
        /// Rows as dictionaries
        /// </summary>
        public IEnumerable<Dictionary<string, string>> AsDictionaries => Rows.Select(row => ToDictionary(row));

        /// <summary>
        /// Get a row as dictionary
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Dictionary</returns>
        public Dictionary<string, string> AsDictionary(int index)
            => index < 0 || index >= CountColumns ? throw new ArgumentException("Invalid index", nameof(index)) : ToDictionary(Rows[index]);

        /// <summary>
        /// Create a dictionary from a row
        /// </summary>
        /// <param name="row">Row</param>
        /// <returns>Dictionary</returns>
        private Dictionary<string, string> ToDictionary(string[] row)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(CountColumns);
            for (int i = 0; i < CountColumns; res[Header[i]] = row[i], i++) ;
            return res;
        }
    }
}
#endif
