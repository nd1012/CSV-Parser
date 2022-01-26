#if !NO_MAP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace wan24.Data
{
    public static partial class CsvParser
    {
        /// <summary>
        /// Object value factories
        /// </summary>
        public static Dictionary<Type, Func<string, object>> ObjectValueFactories { get; } = new Dictionary<Type, Func<string, object>>()
        {
            {typeof(bool),(value)=>value!=null&&(value=="1"||value.ToLower()=="true") },
            {typeof(int),(value)=>int.Parse(value) },
            {typeof(float),(value)=>float.Parse(value) },
            {typeof(char),(value)=>value[0] },
            {typeof(byte[]),(value)=>value==string.Empty?null:Convert.FromBase64String(value) }
        };

        /// <summary>
        /// Row value factories
        /// </summary>
        public static Dictionary<Type, Func<object, string>> RowValueFactories { get; } = new Dictionary<Type, Func<object, string>>()
        {
            {typeof(bool),(value)=>value.ToString() },
            {typeof(int),(value)=>value.ToString() },
            {typeof(float),(value)=>value.ToString() },
            {typeof(char),(value)=>value.ToString() },
            {typeof(byte[]),(value)=>value==null?string.Empty:Convert.ToBase64String((byte[])value) }
        };

        /// <summary>
        /// Type mappings
        /// </summary>
        public static Dictionary<Type, CsvMappings> TypeMappings { get; } = new Dictionary<Type, CsvMappings>();

        /// <summary>
        /// Create a mapping
        /// </summary>
        /// <param name="mappings">Mappings</param>
        /// <returns>Mapping</returns>
        public static CsvMappings CreateMapping(params CsvMapping[] mappings)
        {
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));
            CsvMappings res = new CsvMappings();
            foreach (CsvMapping map in mappings) res[map.Field] = map;
            return res;
        }

        /// <summary>
        /// Create a mapping from an object which has properties using the <c>CsvMappingAttribute</c> (and store it to <c>TypeMappings</c>)
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <returns>Mapping</returns>
        public static CsvMappings CreateMapping<T>() => CreateMapping(typeof(T));

        /// <summary>
        /// Create a mapping from an object which has properties using the <c>CsvMappingAttribute</c> (and store it to <c>TypeMappings</c>)
        /// </summary>
        /// <param name="type">Object type</param>
        /// <returns>Mapping</returns>
        public static CsvMappings CreateMapping(Type type)
        {
            if (TypeMappings.ContainsKey(type)) return TypeMappings[type];
            CsvMappings res = new CsvMappings();
            CsvMappingAttribute attr;
            foreach (PropertyInfo pi in from pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        where pi.GetCustomAttribute<CsvMappingAttribute>() != null
                                        select pi)
            {
                attr = pi.GetCustomAttribute<CsvMappingAttribute>();
                res[attr.Field] = new CsvMapping()
                {
                    Field = attr.Field,
                    PropertyName = pi.Name,
                    ObjectValueFactory = ObjectValueFactories.ContainsKey(pi.PropertyType) ? ObjectValueFactories[pi.PropertyType] : null,
                    RowValueFactory = RowValueFactories.ContainsKey(pi.PropertyType) ? RowValueFactories[pi.PropertyType] : null
                };
            }
            TypeMappings[type] = res;
            return res;
        }

        /// <summary>
        /// Map a row to an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="row">Row</param>
        /// <param name="mapping">Mapping</param>
        /// <returns>Object</returns>
        public static T Map<T>(string[] row, Dictionary<int, CsvMapping> mapping = null) where T : new() => (T)Map(typeof(T), row, mapping);

        /// <summary>
        /// Map a row to an object
        /// </summary>
        /// <param name="type">Object type</param>
        /// <param name="row">Row</param>
        /// <param name="mapping">Mapping</param>
        /// <returns>Object</returns>
        public static object Map(Type type, string[] row, Dictionary<int, CsvMapping> mapping = null)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (mapping == null) mapping = CreateMapping(type);
            object res = Activator.CreateInstance(type);
            PropertyInfo pi;
            object value;
            foreach (CsvMapping map in mapping.Values)
            {
                pi = type.GetProperty(map.PropertyName, BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException("Failed to map to property", nameof(mapping));
                if (map.PreValidation != null && !map.PreValidation(row[map.Field])) throw new InvalidDataException("Field not pre-validated");
                value = map.ObjectValueFactory == null ? row[map.Field] : map.ObjectValueFactory(row[map.Field]);
                if (map.PostValidation != null && !map.PostValidation(value)) throw new InvalidDataException("Field not post-validated");
                pi.SetValue(res, value);
            }
            return res;
        }

        /// <summary>
        /// Map a row to an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="row">Row</param>
        /// <param name="obj">Object</param>
        /// <param name="mapping">Mapping</param>
        /// <returns>Object</returns>
        public static T Map<T>(string[] row, T obj, Dictionary<int, CsvMapping> mapping = null)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (mapping == null) mapping = CreateMapping<T>();
            Type type = typeof(T);
            PropertyInfo pi;
            object value;
            foreach (CsvMapping map in mapping.Values)
            {
                pi = type.GetProperty(map.PropertyName, BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException("Failed to map to property", nameof(mapping));
                if (map.PreValidation != null && !map.PreValidation(row[map.Field])) throw new InvalidDataException("Field not pre-validated");
                value = map.ObjectValueFactory == null ? row[map.Field] : map.ObjectValueFactory(row[map.Field]);
                if (map.PostValidation != null && !map.PostValidation(value)) throw new InvalidDataException("Field not post-validated");
                pi.SetValue(obj, value);
            }
            return obj;
        }

        /// <summary>
        /// Unmap an object to a row
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object</param>
        /// <param name="mapping">Mapping</param>
        /// <returns>Row</returns>
        public static string[] Unmap<T>(T obj, Dictionary<int, CsvMapping> mapping = null) => Unmap((object)obj, mapping);

        /// <summary>
        /// Unmap an object to a row
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="mapping">Mapping</param>
        /// <returns>Row</returns>
        public static string[] Unmap(object obj, Dictionary<int, CsvMapping> mapping = null)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            Type type = obj.GetType();
            if (mapping == null) mapping = CreateMapping(type);
            List<string> res = new List<string>();
            PropertyInfo pi;
            foreach (CsvMapping map in from map in mapping.Values
                                       orderby map.Field
                                       select map)
            {
                pi = type.GetProperty(map.PropertyName, BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException("Failed to map to property", nameof(mapping));
                res.Add(map.RowValueFactory == null ? pi.GetValue(obj)?.ToString() : map.RowValueFactory(pi.GetValue(obj)));
            }
            return res.ToArray();
        }
    }
}
#endif
