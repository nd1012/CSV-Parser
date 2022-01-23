using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using wan24.Data;

namespace CSV_Parser_Tests
{
    [TestClass]
    public class CsvParser_Tests
    {
        private static readonly CsvTable Data;

        static CsvParser_Tests()
        {
            Data = new CsvTable();
            Data.Header.AddRange(new string[]
            {
                "col1",
                "col2"
            });
            Data.Rows.AddRange(new string[][]
            {
                new string[]{"a","b"},
                new string[]{"c","d"},
                new string[]{"e","f"}
            });
        }

        [TestMethod]
        public void CsvParser_Test()
        {
            string csvData = Data.ToString(true);
            CsvTable table = CsvParser.ParseString(csvData);
            CompareTable(table);
            table = CsvParser.ParseStream(new MemoryStream(Encoding.Default.GetBytes(csvData)));
            CompareTable(table);
            string[] header = CsvParser.ParseHeaderFromString(csvData);
            CompareHeader(header);
            header = CsvParser.ParseHeaderFromStream(new MemoryStream(Encoding.Default.GetBytes(csvData)));
            CompareHeader(header);
            Assert.AreEqual(CsvParser.CountRowsFromString(csvData), Data.CountRows + 1);
            Assert.AreEqual(CsvParser.CountRowsFromStream(new MemoryStream(Encoding.Default.GetBytes(csvData))), Data.CountRows + 1);
        }

        [TestMethod]
        public async Task CsvParserAsync_Test()
        {
            CompareTable(await CsvParser.ParseStreamAsync(new MemoryStream(Encoding.Default.GetBytes(Data.ToString(true)))));
            CompareHeader(await CsvParser.ParseHeaderFromStreamAsync(new MemoryStream(Encoding.Default.GetBytes(Data.ToString(true)))));
            Assert.AreEqual(await CsvParser.CountRowsFromStreamAsync(new MemoryStream(Encoding.Default.GetBytes(Data.ToString(true)))), Data.CountRows + 1);
        }

        private static void CompareTable(CsvTable table)
        {
            CompareHeader(table.Header.ToArray());
            Assert.AreEqual(table.CountRows, Data.CountRows);
            for (int i = 0; i < Data.CountRows; i++)
            {
                Assert.AreEqual(table.Rows[i].Length, Data.Rows[i].Length);
                for (int j = 0; j < Data.Rows[i].Length; j++)
                    Assert.AreEqual(table.Rows[i][j], Data.Rows[i][j]);
            }
        }

        private static void CompareHeader(string[] header)
        {
            Assert.AreEqual(header.Length, Data.CountColumns);
            for (int i = 0; i < Data.CountColumns; Assert.AreEqual(header[i], Data.Header[i]), i++) ;
        }
    }
}
