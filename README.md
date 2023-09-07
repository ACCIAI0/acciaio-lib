# acciaio-lib
C# Library born from necessities and released for whoever finds out they have those same necessities. It is composed of a number of modules that can be used separately from one another. At the moment the only available module is `Acciaio.Data`.

## `Acciaio.Data`
Contains an intuitive structure to load, parse and read CSV files or strings. The main types are:

* `Csv` - The main type of the module. It represents a CSV file and allows to read values from its rows by accessing them by header or index. It's a collection of `CsvColumn`. <br>It also exposes static methods for building an instance of it through Fluent API.
* `CsvColumn` - Represents a single column of a CSV file and allows to change it's header, position in the CSV and content. It's a collection of `CsvCell`.
* `CsvRow` - Represents a single row of a CSV file and it's a collection of `CsvCell`. It's a view on the real structure of a `Csv` and as such does not allow the same editing freedom of a `CsvColumn`.
* `CsvCell` - Represents a single value in a CSV file. It allows to access it as string, DateTime, Enum value or as an integer, single-precision or double-precision decimal number.
* `ICsvTypeMapper` - This interface allows users to create custom mappers from a `CsvRow` to a custom type.
* `CsvTypeMapperAttribute` (`CsvTypeMapperAttribute<T>` for .NET7+) - This attribute can be assigned to classes in order to specify what concrete implementation of `ICsvTypeMapper` is used when calling any overload of `Csv.MapToType<T>()`. If a specific type is used without this attribute, a default mapper is used.
* `CsvHeaderMapperAttribute` - This attribute can be assigned to fields and properties (with write access) and it is used by the default mapper to better assign values. If not present, the name of the field/property is used as mapping to the headers. CSV structures without headers will just follow the definition order. 
