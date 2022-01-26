#if !NO_STREAM
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wan24.Data
{
    public partial class CsvStream
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
        /// Peek the first field of the next row
        /// </summary>
        /// <returns>Field or <see langword="null"/></returns>
        public string PeekField()
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
                for (; !found && BufferIndex < len; found = (char)ReadBuffer[BufferIndex] == FieldDelimiter || ReadBuffer[BufferIndex] == 10, BufferIndex++) ;
                if (!found) continue;
                string res = (char)ReadBuffer[BufferIndex] == FieldDelimiter
                    ? CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, BufferIndex, len), FieldDelimiter, StringDelimiter)[0]
                    : null;
                BufferIndex = 0;
                return res;
            }
            return null;
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
        /// Peek the first field of the next row
        /// </summary>
        /// <returns>Field or <see langword="null"/></returns>
        public async Task<string> PeekFieldAsync()
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
                for (; !found && BufferIndex < len; found = (char)ReadBuffer[BufferIndex] == FieldDelimiter || ReadBuffer[BufferIndex] == 10, BufferIndex++) ;
                if (!found) continue;
                string res = (char)ReadBuffer[BufferIndex] == FieldDelimiter
                    ? CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, BufferIndex - 1, len), FieldDelimiter, StringDelimiter)[0]
                    : null;
                BufferIndex = 0;
                return res;
            }
            return null;
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
#endif
