using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace wan24.Data
{
    public static partial class CsvParser
    {
		/// <summary>
		/// Parse a file and determine the number of CSV table rows
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static int CountRowsFromFile(
			string fileName,
			char? stringDelimiter = '"',
			Encoding encoding = null
			)
			=> CountRowsFromStream(File.OpenRead(fileName), stringDelimiter, encoding: encoding);

		/// <summary>
		/// Parse a file and determine the number of CSV table rows
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static async Task<int> CountRowsFromFileAsync(
			string fileName,
			char? stringDelimiter = '"',
			Encoding encoding = null
			)
			=> await CountRowsFromStreamAsync(File.OpenRead(fileName), stringDelimiter, encoding: encoding);

		/// <summary>
		/// Parse a stream and determine the number of CSV table rows
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static int CountRowsFromStream(
			Stream stream,
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
		public static async Task<int> CountRowsFromStreamAsync(
			Stream stream, char?
			stringDelimiter = '"',
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
	}
}
