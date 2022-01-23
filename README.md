# CSV-Parser

A lightweight .NET Standard 2.0 CSV table parser.

## Usage

Parse a file:

```cs
CsvTable table = wan24.Data.CsvParser.ParseFile(@"path\to\file.csv");
```

Parse a file asynchronous:

```cs
CsvTable table = await wan24.Data.CsvParser.ParseFileAsync(@"path\to\file.csv");
```

Parse a stream:

```cs
CsvTable table = wan24.Data.CsvParser.ParseStream(anyStream);
```

Parse a stream asynchronous:

```cs
CsvTable table = await wan24.Data.CsvParser.ParseStreamAsync(anyStream);
```

Parse a string:

```cs
CsvTable table = wan24.Data.CsvParser.ParseString(csvDataString);
```

You may adjust these details using additional parameters:

- If the first line contains the column headers
- The field delimiter character (comma (`,`) per default)
- The string value delimiter character (double quotes (`"`) per default)
- String encoding to use (default is the default .NET encoding)
- If the stream should be left open (default is `false`)

The resulting CSV table object holds the parsed table data:

- `CountColumns`: column count
- `CountRows`: row count
- `Header`: column headers
- `Rows`: row data

The overloaded `ToString` method would create CSV table data from a CSV table.
