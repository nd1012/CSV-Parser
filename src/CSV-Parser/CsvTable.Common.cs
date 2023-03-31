using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace wan24.Data
{
    public partial class CsvTable
    {
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
        /// Move a column
        /// </summary>
        /// <param name="currentIndex">Current index</param>
        /// <param name="newIndex">New index</param>
        /// <param name="updateMapping">Update the mapping?</param>
        /// <returns>This</returns>
        public CsvTable MoveColumn(int currentIndex, int newIndex, bool updateMapping = true)
        {
            if (currentIndex < 0 || currentIndex >= CountColumns) throw new ArgumentException("Invalid current index", nameof(currentIndex));
            if (newIndex < 0 || newIndex >= CountColumns) throw new ArgumentException("Invalid new index", nameof(newIndex));
            if (currentIndex == newIndex) return this;
            string[] MoveIndex(string[] data) => (currentIndex < newIndex
                    ? data.Take(currentIndex).Concat(data.Skip(currentIndex + 1).Take(newIndex - currentIndex).Append(data[currentIndex])).Concat(data.Skip(newIndex + 1))
                    : data.Take(newIndex).Append(data[currentIndex]).Concat(data.Skip(newIndex + 1).Take(currentIndex - newIndex)).Concat(data.Skip(currentIndex + 1)))
                    .ToArray();
            Header = new List<string>(MoveIndex(Header.ToArray()));
            for (int i = 0; i < CountRows; Rows[i] = MoveIndex(Rows[i]), i++) ;
#if !NO_MAP
            if (updateMapping && Mapping != null)
            {
                foreach (CsvMapping map in Mapping.Values)
                    if (currentIndex < newIndex)
                    {
                        if (map.Field == currentIndex)
                        {
                            map.Field = newIndex;
                        }
                        else if (map.Field >= newIndex)
                        {
                            map.Field++;
                        }
                        else if (map.Field > currentIndex)
                        {
                            map.Field--;
                        }
                    }
                    else
                    {
                        if (map.Field == newIndex)
                        {
                            map.Field = currentIndex;
                        }
                        else if (map.Field >= currentIndex)
                        {
                            map.Field--;
                        }
                        else if (map.Field > newIndex)
                        {
                            map.Field++;
                        }
                    }
                Mapping = CsvParser.CreateMapping(Mapping.Values.ToArray());
            }
#endif
            return this;
        }

        /// <summary>
        /// Swap a column
        /// </summary>
        /// <param name="a">Index A</param>
        /// <param name="b">Index B</param>
        /// <param name="updateMapping">Update the mapping?</param>
        /// <returns>This</returns>
        public CsvTable SwapColumn(int a, int b, bool updateMapping = true)
        {
            if (a < 0 || a >= CountColumns) throw new ArgumentException("Invalid A index", nameof(a));
            if (b < 0 || b >= CountColumns) throw new ArgumentException("Invalid B index", nameof(b));
            if (a == b) return this;
            string dataA;
            string[] SwapIndex(string[] data)
            {
                dataA = data[a];
                data[a] = data[b];
                data[b] = dataA;
                return data;
            }
            Header = new List<string>(SwapIndex(Header.ToArray()));
            foreach (string[] row in Rows) SwapIndex(row);
#if !NO_MAP
            if (updateMapping && Mapping != null)
            {
                if (Mapping.ContainsKey(a)) Mapping[a].Field = b;
                if (Mapping.ContainsKey(b)) Mapping[b].Field = a;
                Mapping = CsvParser.CreateMapping(Mapping.Values.ToArray());
            }
#endif
            return this;
        }

        /// <summary>
        /// Re-order columns
        /// </summary>
        /// <param name="newIndexes">New indexes</param>
        /// <param name="updateMapping">Update the mapping?</param>
        /// <returns>This</returns>
        public CsvTable ReorderColumns(int[] newIndexes, bool updateMapping = true)
        {
            if (newIndexes == null) throw new ArgumentNullException(nameof(newIndexes));
            if (newIndexes.Length != CountColumns) throw new ArgumentException("Index count mismatch", nameof(newIndexes));
            string[] ReorderIndex(string[] data)
            {
                string[] res = new string[data.Length];
                for (int i = 0; i < data.Length; res[newIndexes[i]] = data[i], i++) ;
                return res;
            }
            Header = new List<string>(ReorderIndex(Header.ToArray()));
            for (int i = 0; i < CountRows; Rows[i] = ReorderIndex(Rows[i]), i++) ;
#if !NO_MAP
            if (updateMapping && Mapping != null)
            {
                foreach (CsvMapping map in Mapping.Values) map.Field = newIndexes[map.Field];
                Mapping = CsvParser.CreateMapping(Mapping.Values.ToArray());
            }
#endif
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
    }
}
