# acciaio-lib
C# Library born from necessities and released for whoever finds out they have those same necessities. It is composed of a number of modules that can be used separately from one another. At the moment the available modules are `Acciaio.Data` and `Acciaio.Logic`.

## `Acciaio.Data`
Contains an intuitive structure to load, parse and read CSV files or strings. The main types are:

* `Csv` - The main type of the module. It represents a CSV file and allows to read values from its rows by accessing them by header or index. It's a collection of `CsvColumn`. <br>It also exposes static methods for building an instance of it through Fluent API.
* `CsvColumn` - Represents a single column of a CSV file and allows to change it's header, position in the CSV and content. It's a collection of `CsvCell`.
* `CsvRow` - Represents a single row of a CSV file and it's a collection of `CsvCell`. It's a view on the real structure of a `Csv` and as such does not allow the same editing freedom of a `CsvColumn`.
* `CsvCell` - Represents a single value in a CSV file. It allows to access it as string, DateTime, Enum value or as an integer, single-precision or double-precision decimal number.
* `ICsvTypeMapper` - This interface allows users to create custom mappers from a `CsvRow` to a custom type.
* `CsvTypeMapperAttribute` (`CsvTypeMapperAttribute<T>` for .NET7+) - This attribute can be assigned to classes in order to specify what concrete implementation of `ICsvTypeMapper` is used when calling any overload of `Csv.MapToType<T>()`. If a type without this attribute is used, a default mapper will populate each instance through reflection.
* `CsvHeaderMapperAttribute` - This attribute can be assigned to fields and properties (with write access) and it is used by the default mapper to assign values to said fields and properties. If not present, the name of the field/property is used as mapping to the headers. CSV structures without headers will just follow the definition order. These attributes are not required if the parent type has a CsvTypeMapperAttribute.

## `Acciaio.Logic`
Contains data structures and tools to build complex logic. The main types are:

* `StateAutomaton` - Represents a Finite State Machine. It is composed of `State`s and transitions between them. Each state can be assigned to any  number of transitions, such as **Sequential Transitions**, **Conditional Transitions** or **Backwards Transitions**. Any number of **Global Transitions** can also be specified that move the Automaton from any state to a specified new state. The automaton can be updated manually or in a loop to evolve it's internal state following the transitions that were defined when constructed. On each update, only one transition can occur, even though the conditions are met for multiple subsequent transitions.
* `State` - An abstract base class defining a State Automaton's state. Classes deriving from State must override callbacks for when the automaton enters the state, exits the state or is updated. In case of states used in sequential transitions, it's important to specify a finishing point by setting to `true` the `FinishedExecution` flag (it also must be explicitly resetted on exit. This allows the user to have maximum freedon in how to implement a state.)
*  `AsyncAutomaton` - Every StateAutomaton can be accessed asynchonously and *ticked* on a different thred by using
```C#
Automaton.AsAsync.Tick(callback)
```
