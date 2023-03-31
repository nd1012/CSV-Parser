using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace wan24.Data
{
    /// <summary>
    /// CSV table
    /// </summary>
    public partial class CsvTable : IEnumerable<string[]>, ICloneable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hasHeader">Has a header row?</param>
        /// <param name="fieldDelimiter">Field delimiter</param>
        /// <param name="stringDelimiter">String delimiter</param>
        /// <param name="header">Column headers</param>
        /// <param name="rows">Rows (references will be used, if possible)</param>
        public CsvTable(
            bool? hasHeader = null,
            char fieldDelimiter = ',',
            char? stringDelimiter = '"',
            IEnumerable<string> header = null,
            IEnumerable<IEnumerable<string>> rows = null
            )
        {
            HasHeader = hasHeader ?? header != null;
            FieldDelimiter = fieldDelimiter;
            StringDelimiter = stringDelimiter;
            if (header?.Any() ?? false) Header.AddRange(header);
            if (!(rows?.Any() ?? false)) return;
            if (CountColumns < 1) Header = Enumerable.Range(0, rows.First().Count()).Select(i => i.ToString()).ToList();
            Rows = (rows as List<string[]>) ?? (rows as string[][])?.ToList() ?? new List<string[]>(rows.Count());
            if (Rows.Count > 0) return;
            string[] r;
            foreach (IEnumerable<string> row in rows)
            {
                r = row as string[] ?? row.ToArray();
                if (!CsvParser.IgnoreErrors && r.Length != CountColumns)
                    throw new InvalidDataException($"Row at index #{CountRows} has {r.Length} columns ({CountColumns} expected)");
                Rows.Add(r);
            }
        }

        /// <summary>
        /// Get a row
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Row</returns>
        public string[] this[int index]
            => index < 0 || index >= CountRows
                ? throw new ArgumentException("Invalid index", nameof(index)) 
                : Rows[index];

        /// <summary>
        /// Has a header row?
        /// </summary>
        public bool HasHeader { get; }

        /// <summary>
        /// Field delimiter
        /// </summary>
        public char FieldDelimiter { get; }

        /// <summary>
        /// String delimiter
        /// </summary>
        public char? StringDelimiter { get; }

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
        public List<string> Header { get; private set; } = new List<string>();

        /// <summary>
        /// Rows
        /// </summary>
        public List<string[]> Rows { get; private set; } = new List<string[]>();

        /// <inheritdoc/>
        public IEnumerator<string[]> GetEnumerator() => Rows.GetEnumerator();

        /// <inheritdoc/>
        public object Clone()
        {
            CsvTable res = new CsvTable(HasHeader, FieldDelimiter, StringDelimiter)
            {
                Header = new List<string>(Header),
                Rows = new List<string[]>(CountRows)
            };
            res.Rows.AddRange(from row in Rows select row.ToArray());
            return res;
        }

#if !NO_STREAM
        /// <summary>
        /// Get the CSV table data as string
        /// </summary>
        /// <param name="header">If to add the column headers as first row (if <see langword="null"/>, and <c>stringDelimiter</c> is also <see langword="null"/>, <c>stringDelimiter</c> will be set from <c>StringDelimiter</c></param>
        /// <param name="fieldDelimiter">Field delimiter</param>
        /// <param name="stringDelimiter">String delimiter</param>
        /// <returns>CSV table data as string</returns>
        public string ToString(bool? header = null, char? fieldDelimiter = null, char? stringDelimiter = null)
        {
            if (fieldDelimiter == null) fieldDelimiter = FieldDelimiter;
            if (header == null && stringDelimiter == null) stringDelimiter = StringDelimiter;
            using (MemoryStream ms = new MemoryStream())
            using (CsvStream csv = new CsvStream(ms, Header, (char)fieldDelimiter, stringDelimiter))
            {
                foreach (string[] row in Rows) csv.WriteRows(row);
                return csv.StringEncoding.GetString(ms.ToArray());
            }
        }
#endif

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => Rows.GetEnumerator();
    }
}
