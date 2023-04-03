#if !NO_MAP
using System;
using System.Collections.Generic;
using System.Linq;

namespace wan24.Data
{
    public partial class CsvTable
    {
        /// <summary>
        /// Row &lt;-&gt; object mapping
        /// </summary>
        public Dictionary<int, CsvMapping> Mapping { get; set; }

        /// <summary>
        /// Enumerate trough objects
        /// </summary>
        public IEnumerable<object> Objects
        {
            get
            {
                for (int i = 0; i < CountRows; i++) yield return AsObject(i);
            }
        }

        /// <summary>
        /// Get a row as object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="index">Index</param>
        /// <param name="mapping">Override mapping</param>
        /// <returns>Object</returns>
        public T AsObject<T>(int index, Dictionary<int, CsvMapping> mapping = null) where T : new()
        {
            if (index < 0 || index > CountRows) throw new ArgumentException("Invalid index", nameof(index));
            return CsvParser.Map<T>(Rows[index], mapping ?? Mapping);
        }

        /// <summary>
        /// Get rows as object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="index">Index</param>
        /// <param name="mapping">Override mapping</param>
        /// <returns>Object</returns>
        public IEnumerable<T> AsObjects<T>(int index, Dictionary<int, CsvMapping> mapping = null) where T : new()
        {
            if (index < 0 || index > CountRows) throw new ArgumentException("Invalid index", nameof(index));
            foreach (string[] row in Rows) yield return CsvParser.Map<T>(row, mapping ?? Mapping);
        }

        /// <summary>
        /// Get a row as object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="index">Index</param>
        /// <param name="obj">Object</param>
        /// <param name="mapping">Override mapping</param>
        /// <returns>Object</returns>
        public T AsObject<T>(int index, T obj, Dictionary<int, CsvMapping> mapping = null)
        {
            if (index < 0 || index > CountRows) throw new ArgumentException("Invalid index", nameof(index));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return CsvParser.Map(Rows[index], obj, mapping ?? Mapping);
        }

        /// <summary>
        /// Get an object from an object row (having the object type in the first field)
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Object or <see langword="null"/></returns>
        public object AsObject(int index) => Rows[index].Length < 1 ? null : CsvParser.Map(Type.GetType(Rows[index][0]), Rows[index].Skip(1).ToArray());

        /// <summary>
        /// Add rows from objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public CsvTable AddObjects<T>(params T[] objs)
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            foreach (T obj in objs) AddRow(CsvParser.Unmap(obj, Mapping));
            return this;
        }

        /// <summary>
        /// Add rows from objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Override mapping</param>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public CsvTable AddObjects<T>(Dictionary<int, CsvMapping> mapping, params T[] objs)
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            foreach (T obj in objs) AddRow(CsvParser.Unmap(obj, mapping));
            return this;
        }

        /// <summary>
        /// Add rows from objects with the type names in the first field
        /// </summary>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public CsvTable AddObjects(params object[] objs)
        {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            foreach(object obj in objs) Rows.Add(obj == null ? Array.Empty<string>() : new string[] { obj.GetType().FullName }.Concat(CsvParser.Unmap(obj)).ToArray());
            return this;
        }

        /// <summary>
        /// Create a mapping from the column headers (supports string properties only, will set the <c>Mapping</c> property)
        /// </summary>
        /// <returns>This</returns>
        public CsvTable CreateMapping()
        {
            CsvMappings mapping = new CsvMappings();
            for (int i = 0; i < Header.Count; i++)
                mapping[i] = new CsvMapping()
                {
                    Field = i,
                    PropertyName = Header[i]
                };
            Mapping = mapping;
            return this;
        }
    }
}
#endif
