using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace wan24.Data
{
    /// <summary>
    /// CSV table
    /// </summary>
    public class CsvTable
    {
        /// <summary>
        /// Regular expression to match a numeric expression
        /// </summary>
        public static readonly Regex RX_NUMBER = new Regex(@"^(\d+|\d*[\.,]\d+)$", RegexOptions.Compiled);

        /// <summary>
        /// Constructor
        /// </summary>
        public CsvTable() { }

        /// <summary>
        /// Column count
        /// </summary>
        public int CountColumns => Header.Count;

        /// <summary>
        /// Row count
        /// </summary>
        public int CountRows => Rows.Count;

        /// <summary>
        /// Column headers
        /// </summary>
        public List<string> Header { get; } = new List<string>();

        /// <summary>
        /// Rows
        /// </summary>
        public List<string[]> Rows { get; } = new List<string[]>();

        /// <summary>
        /// Get CSV table data
        /// </summary>
        /// <param name="header">If to add the column headers as first row</param>
        /// <param name="fieldDelimiter">Field delimiter</param>
        /// <param name="stringDelimiter">String delimiter</param>
        /// <returns>CSV table data</returns>
        public string ToString(bool header, char fieldDelimiter = ',', char? stringDelimiter = '"')
        {
            StringBuilder sb = new StringBuilder();
            List<string> row = new List<string>();
            void AddRow(List<string> r)
            {
                int len = r.Count;
                for (int i = 0; i < len; i++)
                    if (!RX_NUMBER.IsMatch(r[i]))
                        r[i] = $"{stringDelimiter}{r[i].Replace(stringDelimiter.ToString(), $"{stringDelimiter}{stringDelimiter}")}{stringDelimiter}";
                sb.AppendLine(string.Join(fieldDelimiter.ToString(), r));
                r.Clear();
            }
            if (header)
            {
                row.AddRange(Header);
                AddRow(row);
            }
            foreach(string[] r in Rows)
            {
                row.AddRange(r);
                AddRow(row);
            }
            return sb.ToString();
        }
    }
}
