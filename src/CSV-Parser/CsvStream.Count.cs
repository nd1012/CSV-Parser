#if !NO_STREAM
using System;
using System.Linq;
using System.Threading.Tasks;

namespace wan24.Data
{
    public partial class CsvStream
    {
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
    }
}
#endif
