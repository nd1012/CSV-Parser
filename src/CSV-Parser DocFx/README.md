# CSV-Parser

A lightweight .NET Standard 2.0 CSV table (RFC 4180-like) parser.

## How to get it

CSV-Parser is available as 
(NuGet package "CSV-Parser")[https://www.nuget.org/packages/CSV-Parser/].

## Usage

Parse a file example:

```cs
CsvTable table = wan24.Data.CsvParser.ParseFile(@"/path/to/file.csv");
foreach(string[] row in table)
{
	Console.WriteLine("Row in CSV table:");
	for(int i = 0; i < table.CountColumns; i++)
	{
		Console.WriteLine($"\t{table.Header[i]}: {row[i]}");
	}
}
```

Parse a file asynchronous example:

```cs
CsvTable table = await wan24.Data.CsvParser.ParseFileAsync(@"/path/to/file.csv");
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
- `EnumerateFile` for **enumerating each row** from a CSV file
- `EnumerateStream` for **enumerating each row** from a CSV stream
- `EnumerateString` for **enumerating each row** from a CSV string
- `CreateMap` for **creating mapping informations**
- `Map` for **mapping a row to an object**
- `Unmap` for **mapping an object to a row**

You may adjust these details using additional parameters:

- If the first line contains the column headers (default is `true`)
- The field delimiter character (default is comma (`,`))
- The string value delimiter character (default is double quotes (`"`))
- String encoding to use (default is the .NET encoding)
- If the stream should be left open (default is `false`)
- Buffer size in bytes (number of bytes that need to include all header columns, default is 80K)
- Chunk size in bytes (for filling the buffer, default is 4K)
- Desired row offset (zero based index of the first row to include in the result)
- Maximum number of rows to include in the result (beginning from the row offset)

### CSV table result

The resulting CSV table object holds the parsed table data:

- `CountColumns`: column count
- `CountRows`: row count
- `Header`: column headers
- `Rows`: row data
- `Objects`: objects from rows having their type name in the first field
- `AsDictionaries`: rows as dictionaries (having the headers as key)
- `Mapping`: row <-> object mapping

The overloaded `ToString` method would create CSV table data from a CSV table. Other methods are:

- `CreateHeaders`: create automatic headers (0..n)
- `AddColumn`: add/insert a column (optional using a field value factory)
- `RemoveColumn`: remove a column
- `MoveColumn`: move a column to another position
- `SwapColumn`: swap two columns
- `ReorderColumns`: apply a new column order
- `AddRow`: add a validated row
- `Validate`: validate the CSV table
- `Clear`: clear row (and header) data
- `AsDictionary`: get a row as dictionary
- `Clone`: create a copy of the CSV table object
- `AsObject`: get a row mapped as/to an object
- `AsObjects`: enumerate all rows as objects
- `AddObjects`: map objects to a new row
- `CreateMapping`: create a mapping from the column headers

### Reading/writing CSV data from/to a stream

For memory saving stream operations, you might want to use the `CsvStream`:

```cs
// Reading
using(CsvStream csv = new CsvStream(File.OpenRead(@"path\to\data.csv")))
{
	csv.SkipHeader();// Or ReadHeader (optional, if any)
	foreach(string[] row in csv.Rows)// Or use ReadObjects
	{
		// Enumerate rows or use ReadRow or ReadObject instead
		...
	}
}

// Writing
using(CsvStream csv = new CsvStream(File.OpenWrite(@"path\to\data.csv")))
{
	csv.WriteHeader(new string[] {...});// Optional
	csv.WriteRow(...);// Or use WriteObject(s)
}
```

Find all methods as asynchronous versions, having the `Async` postfix.

For working with dictionaries, you can use the property `AsDictionaries` or the methods `ReadDictionary` and `ReadDictionaryAsync`.

Per default when reading a header/row, the size is limited to 80KB. To adjust this value, you can modify these values at construction time:

- `bufferSize`: Read buffer size in bytes (= maximum header/row size (default: 80KB))
- `chunkSize`: Chunk size in bytes (how many bytes to read before trying to match a header/row from the buffer (default: 4KB))

### Reading/writing objects

In order to be able to read/write objects, you need to define a mapping. This mapping is responsible for telling the CSV-Parser from which property to get a row field value, and to which property to write a field value from a row. The mapping also supports value factories which can convert a value, and value validation.

```cs
Dictionary<int,CsvMapping> mapping = CsvParser.CreateMapping(
	new CsvMapping()
	{
		Field = 0,
		PropertyName = "AnyProperty",
		ObjectValueFactory = ...,// Convert from string to property value (optional)
		RowValueFactory = ...,// Convert from property value to string (optional)
		PreValidation = ...,// Validate a string value from the CSV data
		PostValidation = ...// Validate a converted value before setting it as object property value
	},
	...
);
```

Set this mapping to the `Mapping` property of a `CsvTable`, give it to the `CsvStream` constructor, or as parameter to one of the object mapping methods, if available.

For value conversion, `CsvParser.ObjectValueFactories` and `CsvParser.RowValueFactories` offer default converter functions for these types:

- `bool`
- `int`
- `float`
- `char`
- `byte[]`

You can extend them with any type.

If you want to use the same mapping for the same type everytime when no other mapping was given, you can add a prepared mapping to `CsvParser.TypeMappings`.

In an object you may use the `CsvMappingAttribute` attribute for properties that should be mapped:

```cs
[CsvMapping(0)]
public string PropertyName { get; }
```

The attribute parameter is the index of the related CSV column. Then, for creating a mapping for your object, use `CsvParser.CreateMapping` without parameters. The returned mapping will be stored in the `CsvParser.TypeMappings`.

Actually CSV is used to store a table. Each row has a fixed number of fields, maybe a header row is present. But you can use CSV also for storing mixed data - for example different objects:

```cs
// Assumed all used types are working with CsvMappingAttribute, 
// or mapping are prepared in CsvParser.TypeMappings already

// Writing objects
using(CsvStream csv = new CsvStream(FileStream.OpenWrite("objects.csv")))
{
	csv.WriteObjectRows(anyObjectInstance, anotherTypeInstance);
}

// Reading objects
using(CsvStream csv = new CsvStream(FileStream.OpenRead("objects.csv")))
{
	anyObjectInstance = csv.ReadObjectRow() as AnyType;
	anotherTypeInstance = csv.ReadObjectRow() as AnotherType;
}
```

**NOTE**: The field mapping needs to count from field index zero, because the mapper will get the row without the first field that contains the type name! This ensures that you can re-use the mapping everywhere.

Using the streams `ObjectRows` property you can also enumerate trough objects from a CSV file.

`CsvTable` implements `AsObject`, `AddObjects` and `Objects` for this purpose.

### Ignore errors in CSV data

Usually each row should have the number of fields that equals the number of columns. To ignore, when a row has a different field count:

```cs
CsvParser.IgnoreErrors = true;
```

This setting will also ignore `null` headers/values, and if using `ToString` when a string delimiter is required to produce valid CSV data.

**WARNING**: Ignoring errors may cause unknown failures and produce invalid CSV data!

## Good to know

Even more lightweight versions of this library are available on request. These can come optional without

- dictionary methods
- object mapping
- stream support (and CSV writing support)

That would reduce the functionality to this minimum, which may be enough for supporting a nice CSV import interface only:

- CSV parsing
- CSV header parsing
- CSV file row counting

The resulting DLL file would be smaller than 30KB, if all extended features are excluded.
