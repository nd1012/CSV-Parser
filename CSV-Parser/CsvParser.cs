using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wan24.Data
{
	/// <summary>
	/// CSV parser
	/// </summary>
    public static class CsvParser
    {
		/// <summary>
		/// Parse a file
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>CSV table</returns>
		public static CsvTable ParseFile(string fileName, bool header = true, char fieldDelimiter = ',', char? stringDelimiter = '"', Encoding encoding = null)
			=> ParseStream(File.OpenRead(fileName), header, fieldDelimiter, stringDelimiter, encoding: encoding);

		/// <summary>
		/// Parse a file
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>CSV table</returns>
		public static async Task<CsvTable> ParseFileAsync(string fileName, bool header = true, char fieldDelimiter = ',', char? stringDelimiter = '"', Encoding encoding = null)
			=> await ParseStreamAsync(File.OpenRead(fileName), header, fieldDelimiter, stringDelimiter, encoding: encoding);

		/// <summary>
		/// Parse a stream
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>CSV table</returns>
		public static CsvTable ParseStream(Stream stream, bool header = true, char fieldDelimiter = ',', char? stringDelimiter = '"', bool leaveOpen = false, Encoding encoding = null)
		{
			if (encoding == null) encoding = Encoding.Default;
			using (var closeStream = leaveOpen ? null : stream)
			{
				byte[] buffer = new byte[stream.Length];
				if (buffer.Length < 1) throw new InvalidDataException("No CSV data");
				if (stream.Read(buffer, 0, buffer.Length) != buffer.Length) throw new IOException($"Failed to read {buffer.Length} bytes from stream");
				return ParseString(encoding.GetString(buffer), header, fieldDelimiter, stringDelimiter);
			}
		}

		/// <summary>
		/// Parse a stream
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>CSV table</returns>
		public static async Task<CsvTable> ParseStreamAsync(
			Stream stream, 
			bool header = true, 
			char fieldDelimiter = ',', 
			char? stringDelimiter = '"', 
			bool leaveOpen = false, 
			Encoding encoding = null
			)
		{
			if (encoding == null) encoding = Encoding.Default;
			using (var closeStream = leaveOpen ? null : stream)
			{
				byte[] buffer = new byte[stream.Length];
				if (buffer.Length < 1) throw new InvalidDataException("No CSV data");
				if (await stream.ReadAsync(buffer, 0, buffer.Length) != buffer.Length) throw new IOException($"Failed to read {buffer.Length} bytes from stream");
				return ParseString(encoding.GetString(buffer), header, fieldDelimiter, stringDelimiter);
			}
		}

		/// <summary>
		/// Parse a string
		/// </summary>
		/// <param name="csv">CSV data</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <returns>CSV table</returns>
		public static CsvTable ParseString(string csv, bool header = true, char fieldDelimiter = ',', char? stringDelimiter = '"')
        {
			if (!csv.EndsWith("\n")) csv += "\n";
			char? prev = null, c;
			List<string> row = new List<string>() { string.Empty };
			int col = 0, index = 0;
			bool str = true;
			CsvTable res = new CsvTable();
			foreach(char currentChar in csv)
            {
				c = currentChar;
                if (c == stringDelimiter)
                {
					if (!(str = !str) && c == prev) row[col] += c;
                }
				else if (str && c == fieldDelimiter)
                {
					row.Add(string.Empty);
					c = null;
					col++;
                }
				else if (str && c == '\n')
                {
					if (prev == '\r') row[col] = row[col].Substring(0, row[col].Length - 1);
					if (res.CountColumns < 1)
						if (!header)
						{
							res.Header.AddRange(Enumerable.Range(0, row.Count).Select(i => i.ToString()));
						}
						else
						{
							res.Header.AddRange(row);
							row.Clear();
						}
                    if (row.Count > 0)
                    {
						if (row.Count != res.CountColumns) throw new IndexOutOfRangeException($"Invalid row #{index} length (expected {res.CountColumns} fields, got {row.Count}");
						res.Rows.Add(row.ToArray());
                    }
					c = null;
					row.Clear();
					row.Add(string.Empty);
					col = 0;
					index++;
                }
                else
                {
					row[col] += c;
                }
				prev = c;
            }
			return res;
		}

		/// <summary>
		/// Parse a file and determine the number of CSV table rows
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static int ParseFile(string fileName, char? stringDelimiter = '"', Encoding encoding = null)
			=> CountRowsFromStream(File.OpenRead(fileName), stringDelimiter, encoding: encoding);

		/// <summary>
		/// Parse a file and determine the number of CSV table rows
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static async Task<int> CountRowsFromFileAsync(string fileName, char? stringDelimiter = '"', Encoding encoding = null)
			=> await CountRowsFromStreamAsync(File.OpenRead(fileName), stringDelimiter, encoding: encoding);

		/// <summary>
		/// Parse a stream and determine the number of CSV table rows
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static int CountRowsFromStream(Stream stream, char? stringDelimiter = '"', bool leaveOpen = false, Encoding encoding = null)
		{
			if (encoding == null) encoding = Encoding.Default;
			using (var closeStream = leaveOpen ? null : stream)
			{
				byte[] buffer = new byte[stream.Length];
				if (buffer.Length < 1) throw new InvalidDataException("No CSV data");
				if (stream.Read(buffer, 0, buffer.Length) != buffer.Length) throw new IOException($"Failed to read {buffer.Length} bytes from stream");
				return CountRowsFromString(encoding.GetString(buffer), stringDelimiter);
			}
		}

		/// <summary>
		/// Parse a stream and determine the number of CSV table rows
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static async Task<int> CountRowsFromStreamAsync(Stream stream,char? stringDelimiter = '"',bool leaveOpen = false,Encoding encoding = null)
		{
			if (encoding == null) encoding = Encoding.Default;
			using (var closeStream = leaveOpen ? null : stream)
			{
				byte[] buffer = new byte[stream.Length];
				if (buffer.Length < 1) throw new InvalidDataException("No CSV data");
				if (await stream.ReadAsync(buffer, 0, buffer.Length) != buffer.Length) throw new IOException($"Failed to read {buffer.Length} bytes from stream");
				return CountRowsFromString(encoding.GetString(buffer), stringDelimiter);
			}
		}

		/// <summary>
		/// Parse a string and determine the number of CSV table rows
		/// </summary>
		/// <param name="csv">CSV data</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <returns>Row count</returns>
		public static int CountRowsFromString(string csv, char? stringDelimiter = '"')
		{
			if (!csv.EndsWith("\n")) csv += "\n";
			int res = 0;
			bool str = true;
			foreach (char currentChar in csv)
				if (currentChar == stringDelimiter)
				{
					str = !str;
				}
				else if (str && currentChar == '\n')
				{
					res++;
				}
			return res;
		}

		/// <summary>
		/// Parse a file and return the column headers
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="bufferSize">Buffer size in bytes</param>
		/// <param name="chunkSize">Chunk size in bytes</param>
		/// <returns>Column headers or <see langword="null"/>, if none</returns>
		public static string[] ParseHeaderFromFile(
			string fileName,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			Encoding encoding = null,
			int bufferSize = 81920,
			int chunkSize = 4096
			)
			=> ParseHeaderFromStream(File.OpenRead(fileName), fieldDelimiter, stringDelimiter, encoding: encoding, bufferSize: bufferSize, chunkSize: chunkSize);

		/// <summary>
		/// Parse a file and return the column headers
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="bufferSize">Buffer size in bytes</param>
		/// <param name="chunkSize">Chunk size in bytes</param>
		/// <returns>Column headers or <see langword="null"/>, if none</returns>
		public static async Task<string[]> ParseHeaderFromFileAsync(
			string fileName, 
			char fieldDelimiter = ',', 
			char? stringDelimiter = '"', 
			Encoding encoding = null,
			int bufferSize = 81920,
			int chunkSize = 4096
			)
			=> await ParseHeaderFromStreamAsync(File.OpenRead(fileName), fieldDelimiter, stringDelimiter, encoding: encoding, bufferSize: bufferSize, chunkSize: chunkSize);

		/// <summary>
		/// Parse a stream and return the column headers
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="bufferSize">Buffer size in bytes</param>
		/// <param name="chunkSize">Chunk size in bytes</param>
		/// <returns>Column headers or <see langword="null"/>, if none</returns>
		public static string[] ParseHeaderFromStream(
			Stream stream,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			bool leaveOpen = true,
			Encoding encoding = null,
			int bufferSize = 81920,
			int chunkSize = 4096
			)
		{
			if (encoding == null) encoding = Encoding.Default;
			int index = 0, red, len;
			bool found = false;
			using (var closeStream = leaveOpen ? null : stream)
			{
				byte[] buffer = new byte[bufferSize];
				if (buffer.Length < 1) throw new InvalidDataException("No CSV data");
				for (; !found;)
				{
					len = Math.Min(Math.Min(buffer.Length - index, chunkSize), (int)stream.Length);
					if (len < 1) break;
					red = stream.Read(buffer, index, len);
					if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
					len = index + red;
					for (; !found && index < len; found = buffer[index] == 10, index++) ;
				}
				return found ? ParseHeaderFromString(encoding.GetString(buffer, 0, index), fieldDelimiter, stringDelimiter) : null;
			}
		}

		/// <summary>
		/// Parse a stream and return the column headers
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="bufferSize">Buffer size in bytes</param>
		/// <param name="chunkSize">Chunk size in bytes</param>
		/// <returns>Column headers or <see langword="null"/>, if none</returns>
		public static async Task<string[]> ParseHeaderFromStreamAsync(
			Stream stream,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			bool leaveOpen = true,
			Encoding encoding = null,
			int bufferSize = 81920,
			int chunkSize = 4096
			)
		{
			if (encoding == null) encoding = Encoding.Default;
			int index = 0, red, len;
			bool found = false;
			using (var closeStream = leaveOpen ? null : stream)
			{
				byte[] buffer = new byte[bufferSize];
				if (buffer.Length < 1) throw new InvalidDataException("No CSV data");
				for (; !found;)
				{
					len = Math.Min(Math.Min(buffer.Length - index, chunkSize), (int)stream.Length);
					if (len < 1) break;
					red = await stream.ReadAsync(buffer, index, len);
					if (red != len) throw new IOException($"Failed to read {len} bytes from stream (got {red})");
					len = index + red;
					for (; !found && index < len; found = buffer[index] == 10, index++) ;
				}
				return found ? ParseHeaderFromString(encoding.GetString(buffer, 0, index), fieldDelimiter, stringDelimiter) : null;
			}
		}

		/// <summary>
		/// Parse a string and return the column headers
		/// </summary>
		/// <param name="csv">CSV data</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <returns>Column headers or <see langword="null"/>, if none</returns>
		public static string[] ParseHeaderFromString(string csv, char fieldDelimiter = ',', char? stringDelimiter = '"')
		{
			if (!csv.EndsWith("\n")) csv += "\n";
			char? prev = null, c;
			List<string> row = new List<string>() { string.Empty };
			int col = 0;
			bool str = true;
			foreach (char currentChar in csv)
			{
				c = currentChar;
				if (c == stringDelimiter)
				{
					if (!(str = !str) && c == prev) row[col] += c;
				}
				else if (str && c == fieldDelimiter)
				{
					row.Add(string.Empty);
					c = null;
					col++;
				}
				else if (str && c == '\n')
				{
					if (prev == '\r') row[col] = row[col].Substring(0, row[col].Length - 1);
					return row.ToArray();
				}
				else
				{
					row[col] += c;
				}
				prev = c;
			}
			return null;
		}
	}
}
