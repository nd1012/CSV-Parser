using System;
using System.Collections.Generic;
using System.Reflection;

namespace wan24.Data
{
    public static partial class CsvParser
    {
        /// <summary>
        /// Create a mapping
        /// </summary>
        /// <param name="mappings">Mappings</param>
        /// <returns>Mapping</returns>
        public static Dictionary<int, CsvMapping> CreateMapping(params CsvMapping[] mappings)
        {
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));
            Dictionary<int, CsvMapping> res = new Dictionary<int, CsvMapping>();
            foreach (CsvMapping map in mappings) res[map.Field] = map;
            return res;
        }

        /// <summary>
        /// Map a row to an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Mapping</param>
        /// <param name="row">Row</param>
        /// <returns>Object</returns>
        public static T Map<T>(Dictionary<int, CsvMapping> mapping, string[] row) where T : new()
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            if (row == null) throw new ArgumentNullException(nameof(row));
            T res = new T();
            Type type = typeof(T);
            PropertyInfo pi;
            foreach (CsvMapping map in mapping.Values)
            {
                pi = type.GetProperty(map.PropertyName, BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException("Failed to map to property", nameof(mapping));
                pi.SetValue(res, map.ObjectValueFactory == null ? row[map.Field] : map.ObjectValueFactory(row[map.Field]));
            }
            return res;
        }

        /// <summary>
        /// Map a row to an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Mapping</param>
        /// <param name="row">Row</param>
        /// <param name="obj">Object</param>
        /// <returns>Object</returns>
        public static T Map<T>(Dictionary<int, CsvMapping> mapping, string[] row, T obj)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            Type type = typeof(T);
            PropertyInfo pi;
            foreach (CsvMapping map in mapping.Values)
            {
                pi = type.GetProperty(map.PropertyName, BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException("Failed to map to property", nameof(mapping));
                pi.SetValue(obj, map.ObjectValueFactory == null ? row[map.Field] : map.ObjectValueFactory(row[map.Field]));
            }
            return obj;
        }

        /// <summary>
        /// Unmap an object to a row
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Mapping</param>
        /// <param name="obj">Object</param>
        /// <returns>Row</returns>
        public static string[] Unmap<T>(Dictionary<int, CsvMapping> mapping, T obj)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            List<string> res = new List<string>();
            Type type = typeof(T);
            PropertyInfo pi;
            foreach (CsvMapping map in mapping.Values)
            {
                pi = type.GetProperty(map.PropertyName, BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException("Failed to map to property", nameof(mapping));
                res.Add(map.RowValueFactory == null ? pi.GetValue(obj)?.ToString() : map.RowValueFactory(pi.GetValue(obj)));
            }
            return res.ToArray();
        }
    }
}
