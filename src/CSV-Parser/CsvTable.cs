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
            if (header != null) Header.AddRange(header);
            if (rows == null) return;
            if (CountColumns < 1) Header = Enumerable.Range(0, rows.FirstOrDefault()?.Count() ?? 0).Select(i => i.ToString()).ToList();
            Rows = (rows as List<string[]>) ?? (rows as string[][])?.ToList() ?? new List<string[]>(from row in rows select row.ToArray());
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
        /// Get a field ow a row
        /// </summary>
        /// <param name="index">Row index</param>
        /// <param name="header">Field header name</param>
        /// <returns>Field value</returns>
        public string this[int index, string header]
        {
            get
            {
                string[] row = this[index];
                index = Header.IndexOf(header);
                if (index < 0) throw new ArgumentException("Invalid header", nameof(header));
                if (index >= row.Length) throw new InvalidDataException("Row doesn't seem to contain a value for the requested column");
                return row[index];
            }
        }

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
        public object Clone() => new CsvTable(HasHeader, FieldDelimiter, StringDelimiter)
        {
            Header = new List<string>(Header),
            Rows = new List<string[]>(Rows)
        };

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
            using (CsvStream csv = new CsvStream(ms, Header, fieldDelimiter.Value, stringDelimiter))
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
