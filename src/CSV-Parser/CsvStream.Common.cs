#if !NO_STREAM
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wan24.Data
{
    public partial class CsvStream
    {
        /// <summary>
        /// Buffer for reading
        /// </summary>
        protected readonly byte[] ReadBuffer;
        /// <summary>
        /// Chunk size
        /// </summary>
        protected readonly int ChunkSize;
        /// <summary>
        /// Buffer index
        /// </summary>
        protected int BufferIndex = 0;
        /// <summary>
        /// Buffer position
        /// </summary>
        protected int BufferPosition = 0;
        /// <summary>
        /// Number of columns in the header
        /// </summary>
        protected int? _ColumnCount = null;
        /// <summary>
        /// Has the header been written already?
        /// </summary>
        protected bool HeaderWritten = false;

        /// <summary>
        /// Is the end of the stream and the read buffer
        /// </summary>
        public bool IsEndOfData => Position == Length && BufferIndex == BufferPosition;

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
            header = ValidateHeader(header);
            byte[] headerLine = EncodeRow(header);
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
            int index = 0;
            foreach (IEnumerable<string> row in rows)
            {
                ValidateRow(row, index);
                if (!HeaderWritten) WriteHeader();
                byte[] rowLine = EncodeRow(row); ;
                Write(rowLine, 0, rowLine.Length);
                index++;
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
            if(IsEndOfData) throw new InvalidDataException("No header");
            if (!ReadUntil((byte)'\n') && !IsEndOfData)
                throw new InvalidDataException(BufferIndex == ReadBuffer.Length ? "Header too long" : "No header");
            string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, 0, BufferIndex), FieldDelimiter, StringDelimiter);
            Header = res.ToArray();
            return res;
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
        /// Peek the first field of the current row
        /// </summary>
        /// <returns>Field or <see langword="null"/></returns>
        public string PeekField()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (IsEndOfData || (!ReadUntil((byte)FieldDelimiter, (byte)'\n') && !IsEndOfData))
            {
                if (!IsEndOfData) throw new InvalidDataException("Field too long");
                return null;
            }
            string res = ReadBuffer[BufferIndex - 1] == FieldDelimiter
                ? CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, 0, BufferIndex), FieldDelimiter, StringDelimiter)[0]
                : null;
            BufferIndex = 0;
            return res;
        }

        /// <summary>
        /// Read a row
        /// </summary>
        /// <returns>Row or <see langword="null"/></returns>
        public string[] ReadRow()
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (IsEndOfData || (!ReadUntil((byte)'\n') && !IsEndOfData))
            {
                if (!IsEndOfData) throw new InvalidDataException("Row too long");
                return null;
            }
            string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, 0, BufferIndex), FieldDelimiter, StringDelimiter);
            if (!CsvParser.IgnoreErrors && ColumnCount > 0 && res.Length != ColumnCount) throw new InvalidDataException("Invalid field count");
            return res;
        }

        /// <summary>
        /// Write the header
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteHeaderAsync(IEnumerable<string> header = null, CancellationToken cancellationToken = default)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            header = ValidateHeader(header);
            byte[] headerLine = EncodeRow(header);
            await WriteAsync(headerLine, 0, headerLine.Length, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            HeaderWritten = true;
            return this;
        }

        /// <summary>
        /// Write rows
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="rows">Rows</param>
        /// <returns>This</returns>
        public async Task<CsvStream> WriteRowsAsync(CancellationToken cancellationToken, params IEnumerable<string>[] rows)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            int index = 0;
            foreach (IEnumerable<string> row in rows)
            {
                ValidateRow(row, index);
                if (!HeaderWritten) await WriteHeaderAsync(cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                byte[] rowLine = EncodeRow(row);
                await WriteAsync(rowLine, 0, rowLine.Length, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                index++;
            }
            return this;
        }

        /// <summary>
        /// Read the header
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Column headers</returns>
        public async Task<string[]> ReadHeaderAsync(CancellationToken cancellationToken = default)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Header != null) throw new InvalidOperationException("Having header already - won't overwrite");
            if (IsEndOfData) throw new InvalidDataException("No header");
            if (!await ReadUntilAsync(cancellationToken, (byte)'\n').ConfigureAwait(continueOnCapturedContext: false) && !IsEndOfData)
                throw new InvalidDataException(BufferIndex == ReadBuffer.Length ? "Header too long" : "No header");
            string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, 0, BufferIndex), FieldDelimiter, StringDelimiter);
            Header = res.ToArray();
            return res;
        }

        /// <summary>
        /// Peek the first field of the current row
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Field or <see langword="null"/></returns>
        public async Task<string> PeekFieldAsync(CancellationToken cancellationToken = default)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (IsEndOfData || (!await ReadUntilAsync(cancellationToken, (byte)FieldDelimiter, (byte)'\n').ConfigureAwait(continueOnCapturedContext: false) && !IsEndOfData))
            {
                if (!IsEndOfData) throw new InvalidDataException("Field too long");
                return null;
            }
            string res = ReadBuffer[BufferIndex - 1] == FieldDelimiter
                ? CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, 0, BufferIndex), FieldDelimiter, StringDelimiter)[0]
                : null;
            BufferIndex = 0;
            return res;
        }

        /// <summary>
        /// Read a row
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Row or <see langword="null"/></returns>
        public async Task<string[]> ReadRowAsync(CancellationToken cancellationToken = default)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (IsEndOfData || (!await ReadUntilAsync(cancellationToken, (byte)'\n').ConfigureAwait(continueOnCapturedContext: false) && !IsEndOfData))
            {
                if (!IsEndOfData) throw new InvalidDataException("Row too long");
                return null;
            }
            string[] res = CsvParser.ParseHeaderFromString(StringEncoding.GetString(ReadBuffer, 0, BufferIndex), FieldDelimiter, StringDelimiter);
            if (!CsvParser.IgnoreErrors && ColumnCount > 0 && res.Length != ColumnCount) throw new InvalidDataException("Invalid field count");
            return res;
        }

        /// <summary>
        /// Skip the header line
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>This</returns>
        public async Task<CsvStream> SkipHeaderAsync(CancellationToken cancellationToken = default)
        {
            if (Closed) throw new ObjectDisposedException(GetType().FullName);
            if (Header != null) throw new InvalidOperationException("Having header already");
            await ReadHeaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return this;
        }

        /// <summary>
        /// Get a final field value
        /// </summary>
        /// <param name="field">Field value</param>
        /// <returns>Final field value</returns>
        protected string GetFinalField(string field)
        {
            if (field == null)
            {
                if (CsvParser.IgnoreErrors) return field;
                throw new ArgumentNullException(nameof(field));
            }
            if (!field.Contains(FieldDelimiter) && (StringDelimiter == null || !field.Contains(StringDelimiter.Value))) return field;
            if (StringDelimiter == null)
            {
                if (CsvParser.IgnoreErrors) return field;
                throw new InvalidDataException("String delimiter required");
            }
            return $"{StringDelimiter}{field.Replace(StringDelimiter.ToString(), $"{StringDelimiter}{StringDelimiter}")}{StringDelimiter}";
        }

        /// <summary>
        /// Reset the read buffer
        /// </summary>
        protected void ResetBuffer()
        {
            BufferIndex = 0;
            BufferPosition = 0;
        }

        /// <summary>
        /// Normalize the read buffer
        /// </summary>
        protected void NormalizeReadBuffer()
        {
            if (BufferIndex == 0) return;
            if (BufferPosition == BufferIndex)
            {
                ResetBuffer();
            }
            else
            {
                Array.Copy(ReadBuffer, BufferIndex, ReadBuffer, 0, BufferPosition - BufferIndex);
                BufferPosition -= BufferIndex;
                BufferIndex = 0;
            }
        }

        /// <summary>
        /// Read until a character
        /// </summary>
        /// <param name="stopCharacters">Characters to stop at</param>
        /// <returns>Found?</returns>
        protected bool ReadUntil(params byte[] stopCharacters)
        {
            NormalizeReadBuffer();
            // Process the data chunks
            long length = Length,
                position = Position;
            for (int len, red; ;)
            {
                // Read from the stream, if there's no buffered data available
                if (BufferPosition == BufferIndex)
                {
                    if (position == length) break;// End of data
                    len = (int)Math.Min(Math.Min(ReadBuffer.Length, ChunkSize), length - position);
                    red = BaseStream.Read(ReadBuffer, BufferPosition, len);
                    if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
                    BufferPosition += red;
                    position += red;
                }
                // Find the stop characters in the buffer
                for (; BufferIndex < BufferPosition; BufferIndex++)
                {
                    if (!stopCharacters.Contains(ReadBuffer[BufferIndex])) continue;
                    BufferIndex++;// Place the cursor behind the stop character
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Read until a character
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="stopCharacters">Characters to stop at</param>
        /// <returns>Found?</returns>
        protected async Task<bool> ReadUntilAsync(CancellationToken cancellationToken, params byte[] stopCharacters)
        {
            NormalizeReadBuffer();
            // Process the data chunks
            long length = Length,
                position = Position;
            for (int len, red; ;)
            {
                // Read from the stream, if there's no buffered data available
                if (BufferPosition == BufferIndex)
                {
                    if (position == length) break;// End of data
                    len = (int)Math.Min(Math.Min(ReadBuffer.Length, ChunkSize), length - position);
                    red = await BaseStream.ReadAsync(ReadBuffer, BufferPosition, len, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
                    BufferPosition += red;
                    position += red;
                }
                // Find the stop characters in the buffer
                for (; BufferIndex < BufferPosition; BufferIndex++)
                {
                    if (!stopCharacters.Contains(ReadBuffer[BufferIndex])) continue;
                    BufferIndex++;// Place the cursor behind the stop character
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Validate a row
        /// </summary>
        /// <param name="row">Row</param>
        /// <param name="index">Row index</param>
        /// <returns>Row</returns>
        protected IEnumerable<string> ValidateRow(IEnumerable<string> row, int index)
        {
            if (row == null) throw new ArgumentException($"NULL row #{index}", nameof(row));
            if (CsvParser.IgnoreErrors)
            {
                if (!row.Any()) throw new ArgumentException($"No fields in row #{index}", nameof(row));
            }
            else
            {
                if (Header != null && row.Count() != Header.Count()) throw new ArgumentException($"Invalid field count in row #{index}", nameof(row));
            }
            return row;
        }

        /// <summary>
        /// Validate a header
        /// </summary>
        /// <param name="header">Header</param>
        /// <returns>Header</returns>
        protected IEnumerable<string> ValidateHeader(IEnumerable<string> header)
        {
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
            return header;
        }

        /// <summary>
        /// Encode a row
        /// </summary>
        /// <param name="row">Row</param>
        /// <returns>Encoded row bytes</returns>
        protected byte[] EncodeRow(IEnumerable<string> row) => Encoding.Convert(
            Encoding.Default,
            StringEncoding,
            Encoding.Default.GetBytes($"{string.Join(FieldDelimiter.ToString(), from field in row select GetFinalField(field))}{Environment.NewLine}")
            );
    }
}
#endif
