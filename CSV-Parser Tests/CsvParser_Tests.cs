using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wan24.Data;

namespace CSV_Parser_Tests
{
    [TestClass]
    public class CsvParser_Tests
    {
        private static readonly CsvTable Data = new CsvTable(
            header: new string[]
            {
                "col1",
                "col2"
            },
            rows: new string[][]
            {
                new string[]{"a","b"},
                new string[]{"c","d"},
                new string[]{"e","f"}
            }
            );
        private static readonly Dictionary<int, CsvMapping> Mapping = CsvParser.CreateMapping(
            new CsvMapping()
            {
                Field = 0,
                PropertyName = "Field1"
            },
            new CsvMapping()
            {
                Field = 1,
                PropertyName = "Field2",
                ObjectValueFactory = (value) => value[0],
                RowValueFactory = (value) => value.ToString()
            }
            );

        [TestMethod]
        public void CsvParser_Test()
        {
            // Create CSV table data
            string csvData = Data.ToString();

            // Parse string
            CsvTable table = CsvParser.ParseString(csvData);
            CompareTable(table);

            // Parse stream
            table = CsvParser.ParseStream(new MemoryStream(Encoding.Default.GetBytes(csvData)));
            CompareTable(table);

            // Parse stream with row offset and limit
            table = CsvParser.ParseStream(new MemoryStream(Encoding.Default.GetBytes(csvData)), offset: 1, limit: 1);
            CompareHeader(table.Header.ToArray());
            Assert.AreEqual(table.CountRows, 1);
            CompareRow(1, table.Rows[0]);

            // Parse header from string
            string[] header = CsvParser.ParseHeaderFromString(csvData);
            CompareHeader(header);

            // Parse header from stream
            header = CsvParser.ParseHeaderFromStream(new MemoryStream(Encoding.Default.GetBytes(csvData)), leaveOpen: false);
            CompareHeader(header);

            // Count rows from string
            Assert.AreEqual(CsvParser.CountRowsFromString(csvData), Data.CountRows + 1);

            // Enumerate from string
            Assert.AreEqual(CsvParser.EnumerateString(csvData).Count(), Data.CountRows + 1);

            //TODO Mapping
        }

        [TestMethod]
        public async Task CsvParserAsync_Test()
        {
            // Parse stream
            CompareTable(await CsvParser.ParseStreamAsync(new MemoryStream(Encoding.Default.GetBytes(Data.ToString(true)))));

            // Parse header from stream
            CompareHeader(await CsvParser.ParseHeaderFromStreamAsync(new MemoryStream(Encoding.Default.GetBytes(Data.ToString(true))), leaveOpen: false));

            //TODO Mapping
        }

        [TestMethod]
        public void CsvTable_Test()
        {
            // Clone
            CsvTable table = Data.Clone() as CsvTable;
            CompareTable(table);
            Assert.AreEqual(table.HasHeader, true);
            Assert.AreEqual(table.FieldDelimiter, ',');
            Assert.AreEqual(table.StringDelimiter, '"');

            // Create headers
            table.CreateHeaders();
            CompareStringArrays(table.Header.ToArray(), new string[] { "0", "1" });

            // Add a column
            table.AddColumn("test");
            Assert.AreEqual(table.CountColumns, 3);
            Assert.AreEqual(table[0][2], string.Empty);

            // Remove a column
            table.RemoveColumn(2);
            Assert.AreEqual(table.CountColumns, 2);
            CompareStringArrays(table[0], new string[] { "a", "b" });

            // Insert a column
            table.AddColumn("test", 0);
            Assert.AreEqual(table.CountColumns, 3);
            Assert.AreEqual(table[0][0], string.Empty);
            table.RemoveColumn(0);
            CompareStringArrays(table[0], new string[] { "a", "b" });

            // Add row
            table.AddRow(new string[] { "g", "h" });
            Assert.AreEqual(table.CountRows, 4);

            // Validate
            table.Validate();

            // Clear
            table.Clear();
            Assert.AreEqual(table.CountColumns, 0);
            Assert.AreEqual(table.CountRows, 0);
            Assert.AreEqual(table.HasHeader, true);
            Assert.AreEqual(table.FieldDelimiter, ',');
            Assert.AreEqual(table.StringDelimiter, '"');

            // As dictionary
            table = Data.Clone() as CsvTable;
            Dictionary<string, string> dict = table.AsDictionary(0);
            Assert.IsTrue(dict.ContainsKey("col1"));
            Assert.IsTrue(dict.ContainsKey("col2"));
            Assert.AreEqual(dict["col1"], "a");
            Assert.AreEqual(dict["col2"], "b");
            dict = table.AsDictionaries.Last();
            Assert.IsTrue(dict.ContainsKey("col1"));
            Assert.IsTrue(dict.ContainsKey("col2"));
            Assert.AreEqual(dict["col1"], "e");
            Assert.AreEqual(dict["col2"], "f");

            //TODO Mapping
        }

        private static void CompareTable(CsvTable table)
        {
            CompareHeader(table.Header.ToArray());
            Assert.AreEqual(table.CountRows, Data.CountRows);
            for (int i = 0; i < Data.CountRows; CompareRow(i, table.Rows[i]), i++) ;
        }

        private static void CompareHeader(string[] header)
        {
            Assert.AreEqual(header.Length, Data.CountColumns);
            for (int i = 0; i < Data.CountColumns; Assert.AreEqual(header[i], Data.Header[i]), i++) ;
        }

        private static void CompareRow(int index, string[] row) => CompareStringArrays(Data[index], row);

        private static void CompareStringArrays(string[] a, string[] b)
        {
            Assert.AreEqual(a.Length, b.Length);
            for (int j = 0; j < a.Length; Assert.AreEqual(b[j], a[j]), j++) ;
        }

        private class TestObject
        {
            public string Field1 { get; set; }
            public char Field2 { get; set; }
        }
    }
}
