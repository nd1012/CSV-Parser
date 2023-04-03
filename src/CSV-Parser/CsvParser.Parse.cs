using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wan24.Data
{
	public static partial class CsvParser
    {
#if !NO_STREAM
		/// <summary>
		/// Parse a file
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="offset">Row offset</param>
		/// <param name="limit">Row limit</param>
		/// <returns>CSV table</returns>
		public static CsvTable ParseFile(
			string fileName,
			bool header = true,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			Encoding encoding = null,
			int offset = 0,
			int limit = 0
			)
			=> ParseStream(
				File.OpenRead(fileName),
				header,
				fieldDelimiter,
				stringDelimiter,
				leaveOpen: false,
				encoding,
				offset,
				limit
				);

		/// <summary>
		/// Parse a file
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="offset">Row offset</param>
		/// <param name="limit">Row limit</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>CSV table</returns>
		public static async Task<CsvTable> ParseFileAsync(
			string fileName,
			bool header = true,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			Encoding encoding = null,
			int offset = 0,
			int limit = 0,
			CancellationToken cancellationToken = default
			)
			=> await ParseStreamAsync(
				File.OpenRead(fileName),
				header,
				fieldDelimiter,
				stringDelimiter,
				leaveOpen: false,
				encoding,
				offset,
				limit,
				cancellationToken
				).ConfigureAwait(continueOnCapturedContext: false);

		/// <summary>
		/// Parse a stream
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="offset">Row offset</param>
		/// <param name="limit">Row limit</param>
		/// <returns>CSV table</returns>
		public static CsvTable ParseStream(
			Stream stream,
			bool header = true,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			bool leaveOpen = false,
			Encoding encoding = null,
			int offset = 0,
			int limit = 0
			)
		{
			string[] columns = null;
			using (CsvStream csv = new CsvStream(stream, fieldDelimiter, stringDelimiter, leaveOpen: leaveOpen, encoding: encoding))
			{
				if (header) columns = csv.ReadHeader();
				return new CsvTable(hasHeader: header, fieldDelimiter, stringDelimiter, columns, limit < 1 ? csv.Rows.Skip(offset).ToArray() : csv.Rows.Skip(offset).Take(limit).ToArray());
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
        /// <param name="offset">Row offset</param>
        /// <param name="limit">Row limit</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>CSV table</returns>
        public static async Task<CsvTable> ParseStreamAsync(
			Stream stream,
			bool header = true,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			bool leaveOpen = false,
			Encoding encoding = null,
			int offset = 0,
			int limit = 0,
			CancellationToken cancellationToken = default
			)
		{
			string[] columns = null;
			List<string[]> rows = new List<string[]>();
			int current = 0;
			using (CsvStream csv = new CsvStream(stream, fieldDelimiter, stringDelimiter, leaveOpen: leaveOpen, encoding: encoding))
			{
				if (header) columns = await csv.ReadHeaderAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				for (
					string[] row = await csv.ReadRowAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					row != null;
					row = await csv.ReadRowAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), current++)
				{
					if (current < offset) continue;
					rows.Add(row);
					if (limit > 0 && rows.Count >= limit) break;
				}
				return new CsvTable(hasHeader: header, fieldDelimiter, stringDelimiter, columns, rows);
			}
		}
#endif

		/// <summary>
		/// Parse a string
		/// </summary>
		/// <param name="csv">CSV data</param>
		/// <param name="header">Column headers in the first row?</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="offset">Row offset</param>
		/// <param name="limit">Row limit</param>
		/// <returns>CSV table</returns>
		public static CsvTable ParseString(
			string csv,
			bool header = true,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			int offset = -1,
			int limit = 0
			)
		{
			if (csv.Length < 1 || csv[csv.Length - 1] != '\n') csv = $"{csv}\n";
			if (offset > -1) offset++;
			char? prev = null, c;
			List<string> row = new List<string>() { string.Empty };
			int col = 0, index = 0;
			bool str = true;
			CsvTable res = new CsvTable(header, fieldDelimiter, stringDelimiter);
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
					if (res.CountColumns < 1)
					{
						res.Header.AddRange(header ? row : Enumerable.Range(0, row.Count).Select(i => i.ToString()));
						if (header) row.Clear();
					}
					if (row.Count > 0)
					{
						if (!IgnoreErrors && row.Count != res.CountColumns)
							throw new IndexOutOfRangeException($"Invalid row #{index} length (expected {res.CountColumns} fields, got {row.Count}");
						if (index >= offset)
						{
							res.Rows.Add(row.ToArray());
							if (limit > 0 && res.CountRows >= limit) return res;
						}
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
	}
}
