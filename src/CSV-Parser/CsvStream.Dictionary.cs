#if !NO_STREAM
#if !NO_DICT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            return ReadRow() is string[] row ? ToDictionary(row) : null;
        }

        /// <summary>
        /// Get a row as dictionary
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary or <see langword="null"/></returns>
        public async Task<Dictionary<string, string>> ReadDictionaryAsync(CancellationToken cancellationToken = default)
        {
            if (ColumnCount < 1) throw new InvalidOperationException("No columns");
            return await ReadRowAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) is string[] row ? ToDictionary(row) : null;
        }

        /// <summary>
        /// Create a dictionary from a row
        /// </summary>
        /// <param name="row">Row</param>
        /// <returns>Dictionary</returns>
        protected Dictionary<string, string> ToDictionary(string[] row)
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
