using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace wan24.Data
{
    /// <summary>
    /// CSV table
    /// </summary>
    public class CsvTable : IEnumerable<string[]>, ICloneable
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

        /// <summary>
        /// Rows as dictionaries
        /// </summary>
        public IEnumerable<Dictionary<string, string>> AsDictionaries => Rows.Select(row => ToDictionary(row));

        /// <summary>
        /// Row &lt;-&gt; object mapping
        /// </summary>
        public Dictionary<int,CsvMapping> Mapping { get; set; }

        /// <summary>
        /// Create column headers (0..n)
        /// </summary>
        /// <returns>This</returns>
        public CsvTable CreateHeaders()
        {
            if (CountRows < 1) throw new InvalidOperationException("No rows");
            Header = Enumerable.Range(0, Rows[0].Length).Select(i => i.ToString()).ToList();
            return this;
        }

        /// <summary>
        /// Add a column
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="index">Index</param>
        /// <param name="valueFactory">Value factory</param>
        /// <returns>This</returns>
        public CsvTable AddColumn(string header, int index = -1, Func<int, string> valueFactory = null)
        {
            if (index < 0) index = CountColumns;
            if (string.IsNullOrWhiteSpace(header)) throw new ArgumentException("Header required", nameof(header));
            if (index > CountColumns) throw new ArgumentException("Invalid index", nameof(index));
            if (index == CountColumns)
            {
                Header.Add(header);
                for (int i = 0; i < CountRows; Rows[i] = Rows[i].Append(valueFactory == null ? string.Empty : valueFactory(i)).ToArray(), i++) ;
            }
            else
            {
                Header.Insert(index, header);
                List<string> row;
                for (int i = 0; i < CountRows; i++)
                {
                    row = Rows[i].ToList();
                    row.Insert(index, valueFactory == null ? string.Empty : valueFactory(i));
                    Rows[i] = row.ToArray();
                }
            }
            return this;
        }

        /// <summary>
        /// Remove a column
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>This</returns>
        public CsvTable RemoveColumn(int index)
        {
            if (index < 0 || index >= CountColumns) throw new ArgumentException("Invalid index", nameof(index));
            if (CountColumns == 1) throw new InvalidOperationException("Can't remove all columns");
            Header.RemoveAt(index);
            List<string> row;
            for (int i = 0; i < CountRows; i++)
            {
                row = Rows[i].ToList();
                if (CsvParser.IgnoreErrors && row.Count <= index) continue;
                row.RemoveAt(index);
                Rows[i] = row.ToArray();
            }
            return this;
        }

        /// <summary>
        /// Add a row
        /// </summary>
        /// <param name="row">Row</param>
        /// <returns>This</returns>
        public CsvTable AddRow(IEnumerable<string> row)
        {
            string[] r = row as string[] ?? row?.ToArray() ?? throw new ArgumentException("Row required", nameof(row));
            if (!CsvParser.IgnoreErrors)
            {
                if (CountColumns > 0 && r.Length != CountColumns) throw new ArgumentException($"{CountColumns} columns expected, {r.Length} in row", nameof(row));
                if (r.Contains(null)) throw new ArgumentException("NULL values", nameof(row));
            }
            Rows.Add(r);
            return this;
        }

        /// <summary>
        /// Validate the table and all rows (will throw an exception on error)
        /// </summary>
        /// <returns>This</returns>
        public CsvTable Validate()
        {
            if (!CsvParser.IgnoreErrors)
            {
                if (CountColumns < 1) throw new InvalidDataException("No columns");
                if (Header.Contains(null)) throw new InvalidDataException("Headers contains NULL");
            }
            if (Rows.Contains(null)) throw new InvalidDataException("NULL rows");
            if (!CsvParser.IgnoreErrors)
            {
                if (Rows.Where(row => row.Length != CountColumns).Any()) throw new InvalidDataException("Rows with invalid field count");
                if (Rows.Where(row => row.Contains(null)).Any()) throw new InvalidDataException("Rows with NULL values");
            }
            return this;
        }

        /// <summary>
        /// Clear the data
        /// </summary>
        /// <param name="includingHeader">Including column headers?</param>
        /// <returns>This</returns>
        public CsvTable Clear(bool includingHeader = true)
        {
            if (includingHeader) Header.Clear();
            Rows.Clear();
            return this;
        }

        /// <summary>
        /// Get a row as dictionary
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Dictionary</returns>
        public Dictionary<string, string> AsDictionary(int index)
            => index < 0 || index >= CountColumns ? throw new ArgumentException("Invalid index", nameof(index)) : ToDictionary(Rows[index]);

        /// <summary>
        /// Get a row as object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="index">Index</param>
        /// <returns>Object</returns>
        public T AsObject<T>(int index) where T : new()
        {
            if (index < 0 || index > CountRows) throw new ArgumentException("Invalid index", nameof(index));
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            return CsvParser.Map<T>(Mapping, Rows[index]);
        }

        /// <summary>
        /// Get rows as object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="index">Index</param>
        /// <returns>Object</returns>
        public IEnumerable<T> AsObjects<T>(int index) where T : new()
        {
            if (index < 0 || index > CountRows) throw new ArgumentException("Invalid index", nameof(index));
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            foreach (string[] row in Rows) yield return CsvParser.Map<T>(Mapping, row);
        }

        /// <summary>
        /// Get a row as object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="index">Index</param>
        /// <param name="obj">Object</param>
        /// <returns>Object</returns>
        public T AsObject<T>(int index, T obj)
        {
            if (index < 0 || index > CountRows) throw new ArgumentException("Invalid index", nameof(index));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            return CsvParser.Map(Mapping, Rows[index], obj);
        }

        /// <summary>
        /// Add rows from objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public CsvTable AddObjects<T>(params T[] objs)
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            foreach (T obj in objs) AddRow(CsvParser.Unmap(Mapping, obj));
            return this;
        }

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

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => Rows.GetEnumerator();

        /// <summary>
        /// Create a dictionary from a row
        /// </summary>
        /// <param name="row">Row</param>
        /// <returns>Dictionary</returns>
        private Dictionary<string,string> ToDictionary(string[] row)
        {
            Dictionary<string, string> res = new Dictionary<string, string>(CountColumns);
            for (int i = 0; i < CountColumns; res[Header[i]] = row[i], i++) ;
            return res;
        }
    }
}
