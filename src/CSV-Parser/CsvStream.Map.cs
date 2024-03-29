﻿#if !NO_STREAM
#if !NO_MAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wan24.Data
{
    public partial class CsvStream
    {
        /// <summary>
        /// Object mapping
        /// </summary>
        public Dictionary<int, CsvMapping> Mapping { get; }

        /// <summary>
        /// Enumerate object rows
        /// </summary>
        public IEnumerable<object> ObjectRows
        {
            get
            {
                if (Closed) throw new ObjectDisposedException(GetType().FullName);
                for (; Position < Length;) yield return ReadObjectRow();
            }
        }

        /// <summary>
        /// Write objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public CsvStream WriteObjects<T>(params T[] objs)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            foreach (T obj in objs) WriteRows(CsvParser.Unmap(obj, Mapping));
            return this;
        }

        /// <summary>
        /// Write 
        /// </summary>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public CsvStream WriteObjectRows(params object[] objs)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            byte[] nl = Encoding.Convert(Encoding.Default, StringEncoding, Encoding.Default.GetBytes(Environment.NewLine));
            foreach(object obj in objs)
                if (obj == null)
                {
                    Write(nl, 0, nl.Length);
                }
                else
                {
                    WriteRows(new string[] { obj.GetType().FullName }.Concat(CsvParser.Unmap(obj)).ToArray());
                }
            return this;
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Override mapping</param>
        /// <returns>Object</returns>
        public T ReadObject<T>(Dictionary<int, CsvMapping> mapping = null) where T : new()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            return CsvParser.Map<T>(ReadRow(), mapping ?? Mapping);
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object</param>
        /// <param name="mapping">Override mapping</param>
        /// <returns>Object</returns>
        public T ReadObject<T>(T obj, Dictionary<int, CsvMapping> mapping = null)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            return CsvParser.Map(ReadRow(), obj, mapping ?? Mapping);
        }

        /// <summary>
        /// Read objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Override mapping</param>
        /// <returns>Objects</returns>
        public IEnumerable<T> ReadObjects<T>(Dictionary<int, CsvMapping> mapping = null) where T : new()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            foreach (string[] row in Rows) yield return CsvParser.Map<T>(row, mapping ?? Mapping);
        }

        /// <summary>
        /// Read an object row
        /// </summary>
        /// <returns>Object</returns>
        public object ReadObjectRow()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            string typeName = PeekField();
            if (typeName == null) ReadRow();
            return typeName == null ? null : CsvParser.Map(Type.GetType(typeName) ?? throw new TypeInitializationException(typeName, null), ReadRow().Skip(1).ToArray());
        }

        /// <summary>
        /// Write objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteObjectsAsync<T>(CancellationToken cancellationToken, params T[] objs)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            foreach (T obj in objs) await WriteRowsAsync(cancellationToken, CsvParser.Unmap(obj, Mapping)).ConfigureAwait(continueOnCapturedContext: false);
            return this;
        }

        /// <summary>
        /// Write objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Override mapping</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteObjectsAsync<T>(Dictionary<int, CsvMapping> mapping, CancellationToken cancellationToken, params T[] objs)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            foreach (T obj in objs) await WriteRowsAsync(cancellationToken, CsvParser.Unmap(obj, mapping ?? Mapping)).ConfigureAwait(continueOnCapturedContext: false);
            return this;
        }

        /// <summary>
        /// Write 
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteObjectRowsAsync(CancellationToken cancellationToken, params object[] objs)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            byte[] nl = Encoding.Convert(Encoding.Default, StringEncoding, Encoding.Default.GetBytes(Environment.NewLine));
            foreach (object obj in objs)
                if (obj == null)
                {
                    await WriteAsync(nl, 0, nl.Length, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                }
                else
                {
                    await WriteRowsAsync(cancellationToken, new string[] { obj.GetType().FullName }.Concat(CsvParser.Unmap(obj)).ToArray()).ConfigureAwait(continueOnCapturedContext: false);
                }
            return this;
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="mapping">Override mapping</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object</returns>
        public async Task<T> ReadObjectAsync<T>(Dictionary<int, CsvMapping> mapping = null, CancellationToken cancellationToken = default) where T : new()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            return CsvParser.Map<T>(await ReadRowAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), mapping ?? Mapping);
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object</param>
        /// <param name="mapping">Override mapping</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object</returns>
        public async Task<T> ReadObjectAsync<T>(T obj, Dictionary<int, CsvMapping> mapping = null, CancellationToken cancellationToken = default)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            return CsvParser.Map(await ReadRowAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), obj, mapping ?? Mapping);
        }

        /// <summary>
        /// Read an object row
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Object</returns>
        public async Task<object> ReadObjectRowAsync(CancellationToken cancellationToken = default)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            string typeName = await PeekFieldAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (typeName == null) await ReadRowAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return typeName == null 
                ? null 
                : CsvParser.Map(Type.GetType(typeName) ?? throw new TypeInitializationException(typeName, null), (await ReadRowAsync()).Skip(1).ToArray());
        }
    }
}
#endif
#endif
