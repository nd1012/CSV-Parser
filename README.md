# CSV-Parser

A lightweight .NET Standard 2.0 CSV table (RFC 4180-like) parser.

## Usage

Parse a file example:

```cs
CsvTable table = wan24.Data.CsvParser.ParseFile(@"path\to\file.csv");
```

Parse a file asynchronous example:

```cs
CsvTable table = await wan24.Data.CsvParser.ParseFileAsync(@"path\to\file.csv");
```

These static methods are available:

- `ParseFile` and `ParseFileAsync` for **parsing a CSV file**
- `ParseStream` and `ParseStreamAsync` for **parsing a CSV stream**
- `ParseString` for **parsing a CSV string**
- `CountRowsFromFile` and `CountRowsFromFileAsync` for **counting rows** of a CSV file
- `CountRowsFromStream` and `CountRowsFromStreamAsync` for **counting rows** of a CSV stream
- `CountRowsFromString` for **counting rows** of a CSV string
- `ParseHeaderFromFile` and `ParseHeaderFromFileAsync` for **parsing column headers** from a CSV file
- `ParseHeaderFromStream` and `ParseHeaderFromStreamAsync` for **parsing column headers** from a CSV stream
- `ParseHeaderFromString` for **parsing column headers** from a CSV string

You may adjust these details using additional parameters:

- If the first line contains the column headers
- The field delimiter character (comma (`,`) per default)
- The string value delimiter character (double quotes (`"`) per default)
- String encoding to use (default is the default .NET encoding)
- If the stream should be left open (default is `false`)
- Buffer size in bytes (number of bytes that need to include all header columns, default is 80K)
- Chunk size in bytes (for filling the buffer, default is 4K)

The resulting CSV table object holds the parsed table data:

- `CountColumns`: column count
- `CountRows`: row count
- `Header`: column headers
- `Rows`: row data

The overloaded `ToString` method would create CSV table data from a CSV table.
