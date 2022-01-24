using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wan24.Data
{
    /// <summary>
    /// CSV stream
    /// </summary>
    public class CsvStream : Stream
    {
        /// <summary>
        /// Buffer for reading
        /// </summary>
        private readonly byte[] ReadBuffer;
        /// <summary>
        /// Chunk size
        /// </summary>
        private readonly int ChunkSize;
        /// <summary>
        /// Buffer index
        /// </summary>
        private int BufferIndex = 0;
        /// <summary>
        /// Number of columns in the header
        /// </summary>
        private int? _ColumnCount = null;
        /// <summary>
        /// Was the header written already?
        /// </summary>
        private bool HeaderWritten = false;
        /// <summary>
        /// Is closed?
        /// </summary>
        private bool Closed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseStream">Base stream</param>
        /// <param name="fieldDelimiter">Field delimiter</param>
        /// <param name="stringDelimiter">String delimiter</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <param name="chunkSize">Chunk size in bytes</param>
        /// <param name="leaveOpen">Leave the base stream open when disposing?</param>
        /// <param name="encoding">Encoding</param>
        /// <param name="mapping">Object mapping</param>
        public CsvStream(
            Stream baseStream,
            char fieldDelimiter = ',',
            char? stringDelimiter = '"',
            int bufferSize = 81920,
            int chunkSize = 4096,
            bool leaveOpen = false,
            Encoding encoding = null,
            Dictionary<int,CsvMapping> mapping = null
            )
            : base()
        {
            StringEncoding = encoding ?? Encoding.UTF8;
            Mapping = mapping;
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (bufferSize < chunkSize) throw new ArgumentException("Invalid buffer size", nameof(bufferSize));
            if (chunkSize < 4) throw new ArgumentException("Invalid chunk size", nameof(chunkSize));
            LeaveOpen = leaveOpen;
            FieldDelimiter = fieldDelimiter;
            StringDelimiter = stringDelimiter;
            ReadBuffer = new byte[bufferSize];
            ChunkSize = chunkSize;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseStream">Base stream</param>
        /// <param name="table">CSV table configuration</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <param name="chunkSize">Chunk size in bytes</param>
        /// <param name="leaveOpen">Leave the base stream open when disposing?</param>
        /// <param name="encoding">Encoding</param>
        /// <param name="mapping">Object mapping</param>
        public CsvStream(
            Stream baseStream, 
            CsvTable table, 
            int bufferSize = 81920, 
            int chunkSize = 4096, 
            bool leaveOpen = false, 
            Encoding encoding = null, 
            Dictionary<int, CsvMapping> mapping = null
            )
            : this(baseStream, table?.Header, table?.FieldDelimiter ?? ',', table == null ? '"' : table.StringDelimiter, bufferSize, chunkSize, leaveOpen, encoding, mapping)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseStream">Base stream</param>
        /// <param name="header">Column headers</param>
        /// <param name="fieldDelimiter">Field delimiter</param>
        /// <param name="stringDelimiter">String delimiter</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <param name="chunkSize">Chunk size in bytes</param>
        /// <param name="leaveOpen">Leave the base stream open when disposing?</param>
        /// <param name="encoding">Encoding</param>
        /// <param name="mapping">Object mapping</param>
        public CsvStream(
            Stream baseStream,
            IEnumerable<string> header,
            char fieldDelimiter = ',',
            char? stringDelimiter = '"',
            int bufferSize = 81920,
            int chunkSize = 4096,
            bool leaveOpen = false,
            Encoding encoding = null,
            Dictionary<int, CsvMapping> mapping = null
            )
            : this(baseStream, fieldDelimiter, stringDelimiter, bufferSize, chunkSize, leaveOpen, encoding, mapping)
        {
            Header = header?.ToArray();
            _ColumnCount = header?.Count();
        }

        /// <summary>
        /// Encoding
        /// </summary>
        public Encoding StringEncoding { get; }

        /// <summary>
        /// Object mapping
        /// </summary>
        public Dictionary<int,CsvMapping> Mapping { get; }

        /// <summary>
        /// Base stream
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Leave the base stream open when disposing?
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <summary>
        /// Field delimiter
        /// </summary>
        public char FieldDelimiter { get; }

        /// <summary>
        /// String delimiter
        /// </summary>
        public char? StringDelimiter { get; }

        /// <summary>
        /// Column headers
        /// </summary>
        public IEnumerable<string> Header { get; private set; }

        /// <summary>
        /// Number of columns
        /// </summary>
        public int ColumnCount
        {
            get => _ColumnCount ?? 0;
            set
            {
                if (Header != null) throw new InvalidOperationException("Header set already");
                if (value < 1)
                {
                    _ColumnCount = null;
                }
                else
                {
                    _ColumnCount = value;
                }
            }
        }

        /// <summary>
        /// Enumerable rows
        /// </summary>
        public IEnumerable<string[]> Rows
        {
            get
            {
                if (Closed) throw new ObjectDisposedException(GetType().FullName);
                for (string[] row = ReadRow(); row != null; row = ReadRow()) yield return row;
            }
        }

        /// <inheritdoc/>
        public override bool CanRead => BaseStream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => BaseStream.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => BaseStream.CanWrite;

        /// <inheritdoc/>
        public override long Length => BaseStream.Length;

        /// <inheritdoc/>
        public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

        /// <inheritdoc/>
        public override void Flush() => BaseStream.Flush();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

        /// <inheritdoc/>
        public override void SetLength(long value) => BaseStream.SetLength(value);

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);

        /// <inheritdoc/>
        public override void Close()
        {
            if (Closed) return;
            Closed = true;
            if (!LeaveOpen) BaseStream.Close();
            base.Close();
        }

        /// <summary>
        /// Write the header
        /// </summary>
        /// <param name="header">Header</param>
        /// <returns>This</returns>
        public CsvStream WriteHeader(IEnumerable<string> header = null)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (HeaderWritten) throw new InvalidOperationException("Having header already - won't overwrite");
            if (header == null)
            {
                header = Header ?? throw new ArgumentNullException(nameof(header));
            }
            else
            {
                if (header.Count() < 1) throw new ArgumentException("Missing columns", nameof(header));
                Header = header.ToArray();
                _ColumnCount = Header.Count();
            }
            byte[] headerLine = Encoding.Convert(
                Encoding.Default,
                StringEncoding,
                Encoding.Default.GetBytes($"{string.Join(FieldDelimiter.ToString(), from name in header select GetFinalField(name))}{Environment.NewLine}")
                );
            Write(headerLine, 0, headerLine.Length);
            HeaderWritten = true;
            return this;
        }

        /// <summary>
        /// Write rows
        /// </summary>
        /// <param name="rows">Rows</param>
        /// <returns>This</returns>
        public CsvStream WriteRows(params IEnumerable<string>[] rows)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            foreach (IEnumerable<string> row in rows)
            {
                if (row == null) throw new ArgumentException("NULL row", nameof(rows));
                if (CsvParser.IgnoreErrors)
                {
                    if (!row.Any()) throw new ArgumentException("No fields", nameof(row));
                }
                else
                {
                    if (Header != null && row.Count() != Header.Count()) throw new ArgumentException("Invalid field count", nameof(row));
                }
                if (!HeaderWritten) WriteHeader();
                byte[] rowLine = Encoding.Convert(
                    Encoding.Default,
                    StringEncoding,
                    Encoding.Default.GetBytes($"{string.Join(FieldDelimiter.ToString(), from field in row select GetFinalField(field))}{Environment.NewLine}")
                    );
                Write(rowLine, 0, rowLine.Length);
            }
            return this;
        }

        /// <summary>
        /// Read the header
        /// </summary>
        /// <returns>Column headers</returns>
        public string[] ReadHeader()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Header != null) throw new InvalidOperationException("Having header already - won't overwrite");
            if (BufferIndex > 0)
            {
                Array.Copy(ReadBuffer, BufferIndex, ReadBuffer, 0, ReadBuffer.Length - BufferIndex);
                BufferIndex = 0;
            }
            bool found = false;
            for (int len, red; ;)
            {
                len = Math.Min(Math.Min(ReadBuffer.Length - BufferIndex, ChunkSize), (int)Length);
                if (len < 1) break;
                red = Read(ReadBuffer, BufferIndex, len);
                if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
                len = BufferIndex + red;
                for (; !found && BufferIndex < len; found = ReadBuffer[BufferIndex] == 10, BufferIndex++) ;
                if (!found) continue;
                string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, BufferIndex, len), FieldDelimiter, StringDelimiter);
                Header = res.ToArray();
                return res;
            }
            throw new InvalidDataException(BufferIndex == ReadBuffer.Length ? "Header too long" : "No header");
        }

        /// <summary>
        /// Skip the header line
        /// </summary>
        /// <returns>This</returns>
        public CsvStream SkipHeader()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Header != null) throw new InvalidOperationException("Having header already");
            ReadHeader();
            return this;
        }

        /// <summary>
        /// Read a row
        /// </summary>
        /// <returns>Row or <see langword="null"/></returns>
        public string[] ReadRow()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (BufferIndex > 0)
            {
                Array.Copy(ReadBuffer, BufferIndex, ReadBuffer, 0, ReadBuffer.Length - BufferIndex);
                BufferIndex = 0;
            }
            bool found = false;
            for (int len, red; ;)
            {
                len = Math.Min(Math.Min(ReadBuffer.Length - BufferIndex, ChunkSize), (int)Length);
                if (len < 1) break;
                red = Read(ReadBuffer, BufferIndex, len);
                if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
                len = BufferIndex + red;
                for (; !found && BufferIndex < len; found = ReadBuffer[BufferIndex] == 10, BufferIndex++) ;
                if (!found) continue;
                string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, BufferIndex, len), FieldDelimiter, StringDelimiter);
                if (!CsvParser.IgnoreErrors && ColumnCount > 0 && res.Length != ColumnCount) throw new InvalidDataException("Invalid field count");
                return res;
            }
            return null;
        }

        /// <summary>
        /// Write the header
        /// </summary>
        /// <param name="header">Header</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteHeaderAsync(IEnumerable<string> header = null)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (HeaderWritten) throw new InvalidOperationException("Having header already - won't overwrite");
            if (header == null)
            {
                header = Header ?? throw new ArgumentNullException(nameof(header));
            }
            else
            {
                if (header.Count() < 1) throw new ArgumentException("Missing columns", nameof(header));
                Header = header.ToArray();
                _ColumnCount = Header.Count();
            }
            byte[] headerLine = Encoding.Convert(
                Encoding.Default,
                StringEncoding,
                Encoding.Default.GetBytes($"{string.Join(FieldDelimiter.ToString(), from name in header select GetFinalField(name))}{Environment.NewLine}")
                );
            await WriteAsync(headerLine, 0, headerLine.Length);
            HeaderWritten = true;
            return this;
        }

        /// <summary>
        /// Write rows
        /// </summary>
        /// <param name="rows">Rows</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteRowsAsync(params IEnumerable<string>[] rows)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            foreach (IEnumerable<string> row in rows)
            {
                if (row == null) throw new ArgumentException("NULL row", nameof(row));
                if (CsvParser.IgnoreErrors)
                {
                    if (!row.Any()) throw new ArgumentException("No fields", nameof(row));
                }
                else
                {
                    if (Header != null && row.Count() != Header.Count()) throw new ArgumentException("Invalid field count", nameof(row));
                }
                if (!HeaderWritten) await WriteHeaderAsync();
                byte[] rowLine = Encoding.Convert(
                    Encoding.Default,
                    StringEncoding,
                    Encoding.Default.GetBytes($"{string.Join(FieldDelimiter.ToString(), from field in row select GetFinalField(field))}{Environment.NewLine}")
                    );
                await WriteAsync(rowLine, 0, rowLine.Length);
            }
            return this;
        }

        /// <summary>
        /// Read the header
        /// </summary>
        /// <returns>Column headers</returns>
        public async Task<string[]> ReadHeaderAsync()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Header != null) throw new InvalidOperationException("Having header already - won't overwrite");
            if (BufferIndex > 0)
            {
                Array.Copy(ReadBuffer, BufferIndex, ReadBuffer, 0, ReadBuffer.Length - BufferIndex);
                BufferIndex = 0;
            }
            bool found = false;
            for (int len, red; ;)
            {
                len = Math.Min(Math.Min(ReadBuffer.Length - BufferIndex, ChunkSize), (int)Length);
                if (len < 1) break;
                red = await ReadAsync(ReadBuffer, BufferIndex, len);
                if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
                len = BufferIndex + red;
                for (; !found && BufferIndex < len; found = ReadBuffer[BufferIndex] == 10, BufferIndex++) ;
                if (!found) continue;
                string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, BufferIndex, len), FieldDelimiter, StringDelimiter);
                Header = res.ToArray();
                return res;
            }
            throw new InvalidDataException(BufferIndex == ReadBuffer.Length ? "Header too long" : "No header");
        }

        /// <summary>
        /// Read a row
        /// </summary>
        /// <returns>Row or <see langword="null"/></returns>
        public async Task<string[]> ReadRowAsync()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (BufferIndex > 0)
            {
                Array.Copy(ReadBuffer, BufferIndex, ReadBuffer, 0, ReadBuffer.Length - BufferIndex);
                BufferIndex = 0;
            }
            bool found = false;
            for (int len, red; ;)
            {
                len = Math.Min(Math.Min(ReadBuffer.Length - BufferIndex, ChunkSize), (int)Length);
                if (len < 1) break;
                red = await ReadAsync(ReadBuffer, BufferIndex, len);
                if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
                len = BufferIndex + red;
                for (; !found && BufferIndex < len; found = ReadBuffer[BufferIndex] == 10, BufferIndex++) ;
                if (!found) continue;
                string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, BufferIndex, len), FieldDelimiter, StringDelimiter);
                if (!CsvParser.IgnoreErrors && ColumnCount > 0 && res.Length != ColumnCount) throw new InvalidDataException("Invalid field count");
                return res;
            }
            return null;
        }

        /// <summary>
        /// Skip the header line
        /// </summary>
        /// <returns>This</returns>
        public async Task<CsvStream> SkipHeaderAsync()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Header != null) throw new InvalidOperationException("Having header already");
            await ReadHeaderAsync();
            return this;
        }

        /// <summary>
        /// Get the row count (will reset the stream offset)
        /// </summary>
        /// <returns>Row count</returns>
        public long CountRows()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            long offset = Position;
            try
            {
                return Rows.Count();
            }
            finally
            {
                Position = offset;
            }
        }

        /// <summary>
        /// Get the row count (will reset the stream offset)
        /// </summary>
        /// <returns>Row count</returns>
        public async Task<long> CountRowsAsync()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            long offset = Position;
            try
            {
                long res = 0;
                for (; await ReadRowAsync() != null; res++) ;
                return res;
            }
            finally
            {
                Position = offset;
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
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            foreach (T obj in objs) WriteRows(CsvParser.Unmap(Mapping, obj));
            return this;
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <returns>Object</returns>
        public T ReadObject<T>() where T : new()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            return CsvParser.Map<T>(Mapping, ReadRow());
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object</param>
        /// <returns>Object</returns>
        public T ReadObject<T>(T obj)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            return CsvParser.Map(Mapping, ReadRow(), obj);
        }

        /// <summary>
        /// Read objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <returns>Objects</returns>
        public IEnumerable<T> ReadObjects<T>() where T : new()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            foreach (string[] row in Rows) yield return CsvParser.Map<T>(Mapping, row);
        }

        /// <summary>
        /// Write objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="objs">Objects</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteObjectsAsync<T>(params T[] objs)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            foreach (T obj in objs) await WriteRowsAsync(CsvParser.Unmap(Mapping, obj));
            return this;
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <returns>Object</returns>
        public async Task<T> ReadObjectAsync<T>() where T : new()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            return CsvParser.Map<T>(Mapping, await ReadRowAsync());
        }

        /// <summary>
        /// Read an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object</param>
        /// <returns>Object</returns>
        public async Task<T> ReadObjectAsync<T>(T obj)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Mapping == null) throw new InvalidOperationException("Missing mapping");
            return CsvParser.Map(Mapping, await ReadRowAsync(), obj);
        }

        /// <summary>
        /// Get a final fieldvalue
        /// </summary>
        /// <param name="field">Field value</param>
        /// <returns>Final field value</returns>
        private string GetFinalField(string field)
        {
            if (field == null)
            {
                if (CsvParser.IgnoreErrors) return field;
                throw new ArgumentNullException(nameof(field));
            }
            if (!field.Contains(FieldDelimiter) && (StringDelimiter == null || !field.Contains((char)StringDelimiter))) return field;
            if (StringDelimiter == null)
            {
                if (CsvParser.IgnoreErrors) return field;
                throw new InvalidDataException("String delimiter required");
            }
            return $"{StringDelimiter}{field.Replace(StringDelimiter.ToString(), $"{StringDelimiter}{StringDelimiter}")}{StringDelimiter}";
        }
    }
}
