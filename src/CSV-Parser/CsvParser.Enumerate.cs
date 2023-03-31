using System.Collections.Generic;
using System.IO;
using System.Text;

namespace wan24.Data
{
    public static partial class CsvParser
    {
#if !NO_STREAM
		/// <summary>
		/// Parse a file and enumerate the rows
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>CSV table</returns>
		public static IEnumerable<string[]> EnumerateFile(
			string fileName,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			Encoding encoding = null
			)
			=> EnumerateStream(File.OpenRead(fileName), fieldDelimiter, stringDelimiter, encoding: encoding);

		/// <summary>
		/// Parse a stream
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>CSV table</returns>
		public static IEnumerable<string[]> EnumerateStream(
			Stream stream,
			char fieldDelimiter = ',',
			char? stringDelimiter = '"',
			bool leaveOpen = false,
			Encoding encoding = null
			)
		{
			using (CsvStream csv = new CsvStream(stream, fieldDelimiter, stringDelimiter, leaveOpen: leaveOpen, encoding: encoding))
				foreach (string[] row in csv.Rows)
					yield return row;
		}
#endif

		/// <summary>
		/// Parse a string
		/// </summary>
		/// <param name="csv">CSV data</param>
		/// <param name="fieldDelimiter">Field delimiter</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <returns>CSV table</returns>
		public static IEnumerable<string[]> EnumerateString(string csv, char fieldDelimiter = ',', char? stringDelimiter = '"')
		{
			if (!csv.EndsWith("\n")) csv += "\n";
			char? prev = null, c;
			List<string> row = new List<string>() { string.Empty };
			int col = 0, index = 0;
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
					yield return row.ToArray();
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
		}
	}
}
