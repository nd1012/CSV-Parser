#if !NO_STREAM
#if !NO_DICT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wan24.Data
{
    public partial class CsvStream
    {
        /// <summary>
        /// Rows as dictionaries
        /// </summary>
        public IEnumerable<Dictionary<string, string>> AsDictionaries
        {
            get
            {
                for (Dictionary<string, string> dict = ReadDictionary(); dict != null; dict = ReadDictionary()) yield return dict;
            }
        }

        /// <summary>
        /// Get a row as dictionary
        /// </summary>
        /// <returns>Dictionary or <see langword="null"/></returns>
        public Dictionary<string, string> ReadDictionary()
        {
            if (ColumnCount < 1) throw new InvalidOperationException("No columns");
            string[] row = ReadRow();
            return row == null ? null : ToDictionary(row);
        }

        /// <summary>
        /// Get a row as dictionary
        /// </summary>
        /// <returns>Dictionary or <see langword="null"/></returns>
        public async Task<Dictionary<string, string>> ReadDictionaryAsync()
        {
            if (ColumnCount < 1) throw new InvalidOperationException("No columns");
            string[] row = await ReadRowAsync();
            return row == null ? null : ToDictionary(row);
        }

        /// <summary>
        /// Create a dictionary from a row
        /// </summary>
        /// <param name="row">Row</param>
        /// <returns>Dictionary</returns>
        private Dictionary<string, string> ToDictionary(string[] row)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(ColumnCount);
            string[] header = Header.ToArray();
            for (int i = 0; i < ColumnCount; res[header[i]] = row[i], i++) ;
            return res;
        }
    }
}
#endif
#endif
