#if !NO_STREAM
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace wan24.Data
{
    /// <summary>
    /// CSV stream
    /// </summary>
    public partial class CsvStream : Stream
    {
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
            Encoding encoding = null
#if !NO_MAP
            ,Dictionary<int,CsvMapping> mapping = null
#endif
            )
            : base()
        {
            StringEncoding = encoding ?? Encoding.UTF8;
#if !NO_MAP
            Mapping = mapping;
#endif
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
            Encoding encoding = null
#if !NO_MAP
            ,Dictionary<int, CsvMapping> mapping = null
#endif
            )
            : this(
                  baseStream, 
                  table?.Header, 
                  table?.FieldDelimiter ?? ',', 
                  table == null ? '"' : table.StringDelimiter, 
                  bufferSize, 
                  chunkSize, 
                  leaveOpen, 
                  encoding
#if !NO_MAP
                  ,mapping
#endif
                  )
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
            Encoding encoding = null
#if !NO_MAP
            ,Dictionary<int, CsvMapping> mapping = null
#endif
            )
            : this(
                  baseStream, 
                  fieldDelimiter, 
                  stringDelimiter, 
                  bufferSize, 
                  chunkSize, 
                  leaveOpen, 
                  encoding
#if !NO_MAP
                  ,mapping
#endif
                  )
        {
            Header = header?.ToArray();
            _ColumnCount = header?.Count();
        }

        /// <summary>
        /// Encoding
        /// </summary>
        public Encoding StringEncoding { get; }

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
    }
}
#endif
