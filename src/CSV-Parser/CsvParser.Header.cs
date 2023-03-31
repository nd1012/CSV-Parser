using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace wan24.Data
{
    public static partial class CsvParser
    {
#if !NO_STREAM
		/// <summary>
		/// Parse a file and return the column headers
		/// </summary>
		/// <param name="fileName">Filename</param>
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
			=> ParseHeaderFromStream(
				File.OpenRead(fileName),
				fieldDelimiter,
				stringDelimiter,
				encoding: encoding,
				bufferSize: bufferSize,
				chunkSize: chunkSize
				);

		/// <summary>
		/// Parse a file and return the column headers
		/// </summary>
		/// <param name="fileName">Filename</param>
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
			=> await ParseHeaderFromStreamAsync(
				File.OpenRead(fileName),
				fieldDelimiter,
				stringDelimiter,
				encoding: encoding,
				bufferSize: bufferSize,
				chunkSize: chunkSize
				);

		/// <summary>
		/// Parse a stream and return the column headers
		/// </summary>
		/// <param name="stream">Stream</param>
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
			using (CsvStream csv = new CsvStream(stream, fieldDelimiter, stringDelimiter, bufferSize, chunkSize, leaveOpen, encoding)) return csv.ReadRow();
		}

		/// <summary>
		/// Parse a stream and return the column headers
		/// </summary>
		/// <param name="stream">Stream</param>
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
			using (CsvStream csv = new CsvStream(stream, fieldDelimiter, stringDelimiter, bufferSize, chunkSize, leaveOpen, encoding)) return await csv.ReadRowAsync();
		}
#endif

		/// <summary>
		/// Parse a string and return the column headers
		/// </summary>
		/// <param name="csv">CSV data</param>
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
