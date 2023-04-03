using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wan24.Data
{
    public static partial class CsvParser
    {
#if !NO_STREAM
		/// <summary>
		/// Parse a file and determine the number of CSV table rows
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static long CountRowsFromFile(
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
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Row count</returns>
		public static async Task<long> CountRowsFromFileAsync(
			string fileName,
			char? stringDelimiter = '"',
			Encoding encoding = null,
			CancellationToken cancellationToken = default
			)
			=> await CountRowsFromStreamAsync(File.OpenRead(fileName), stringDelimiter, encoding: encoding, cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

		/// <summary>
		/// Parse a stream and determine the number of CSV table rows
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <returns>Row count</returns>
		public static long CountRowsFromStream(
			Stream stream,
			char? stringDelimiter = '"',
			bool leaveOpen = false,
			Encoding encoding = null
			)
		{
			if (encoding == null) encoding = Encoding.Default;
			using (CsvStream csv = new CsvStream(stream, stringDelimiter: stringDelimiter, leaveOpen: leaveOpen, encoding: encoding))
				return csv.CountRows();
		}

		/// <summary>
		/// Parse a stream and determine the number of CSV table rows
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <param name="leaveOpen">Leave the stream open?</param>
		/// <param name="encoding">String encoding</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Row count</returns>
		public static async Task<long> CountRowsFromStreamAsync(
			Stream stream, char?
			stringDelimiter = '"',
			bool leaveOpen = false,
			Encoding encoding = null,
			CancellationToken cancellationToken = default
			)
		{
			if (encoding == null) encoding = Encoding.Default;
			using (CsvStream csv = new CsvStream(stream, stringDelimiter: stringDelimiter, leaveOpen: leaveOpen, encoding: encoding))
				return await csv.CountRowsAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
#endif

		/// <summary>
		/// Parse a string and determine the number of CSV table rows
		/// </summary>
		/// <param name="csv">CSV data</param>
		/// <param name="stringDelimiter">String delimiter</param>
		/// <returns>Row count</returns>
		public static long CountRowsFromString(string csv, char? stringDelimiter = '"')
		{
			if (!csv.EndsWith("\n")) csv += "\n";
            long res = 0;
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
