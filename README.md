# Lombok.NET
This library is to .NET what Lombok is to Java. 
It generates constructors and other fun stuff using Source Generators for those classes you specify special attributes for. Check out the examples for more info.

### Installation
At least Visual Studio 17.3 (or any JetBrains Rider version) is required for projects using this library. The generators generate code compliant with C# 10.
You can install Lombok.NET either via [NuGet](https://www.nuget.org/packages/Lombok.NET)
```
Install-Package Lombok.NET
```
Or via the .NET Core command-line interface:
```
dotnet add package Lombok.NET
```

### Building and Debugging
To debug a generator, simply set a breakpoint and debug a test. This project uses [Verify](https://github.com/VerifyTests/Verify) for snapshot testing.

## Features

- [Constructors](#constructors)
- ["With" methods](#with-methods)
- [Singletons](#singletons)
- [Lazy](#lazy)
- [INotifyPropertyChanged/INotifyPropertyChanging](#property-change-pattern)
- [Serialization](#serialization)
- [Async overloads](#async-overloads)
- [ToString](#tostring)
- [Freezable pattern](#freezable-pattern)
- [Decorator pattern](#decorator-pattern)

## Usage

### Demo
This demonstrates the generating of the `With` pattern. Simply apply an attribute and the library will do the rest. Remember you are not bound to using fields, but can also use properties and supply the appropriate `MemberType` value to the attribute's constructor.

![LombokNetDemo](https://user-images.githubusercontent.com/14217185/140986601-83424d22-57a5-43cb-a491-9234036d245c.gif)

## Constructors
#### Supported types: Classes, Structs (AllArgsConstructor only)

```c#
[AllArgsConstructor]
public partial class Person {
    private string _name;
    private int _age;
}
```

By supplying the `AllArgsConstructor` attribute and making the type `partial`, you allow the Source Generator to create a constructor for it containing all of the classes private fields.\
If you wish to modify this behavior and would instead like to have a constructor generated off of public properties, you can specify this in the attribute's constructor, e.g.:
```c#
[AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
public partial class Person {
    public string Name { get; set; }
    public int Age { get; set; }
}
```
The default is `Field` for the `MemberType` and `Private` for the `AccessType`.\
It is crucial to make the type `partial`, otherwise the Source Generator will not be able to generate a constructor and will throw an exception.

If you only wish to have a constructor generated containing the required fields or properties, Lombok.NET offers the `RequiredArgsConstructor` attribute. Fields are required if they are `readonly`, properties are required if they don't have a `set` accessor.\
There is also a `NoArgsConstructor` attribute which generates an empty constructor.

## With Methods
#### Supported types: Classes
For modifying objects after they were created, a common pattern using ``With...`` methods is used. Lombok.NET will generate these methods for you based on members in your class. Here's an example:
```c#
[AllArgsConstructor]
[With]
public partial class Person {
    private string _name;
    private int _age;
}

class Program {
    public static void Main() {
        var person = new Person("Steve", 22);
        person = person.WithName("Collin");
        
        Console.WriteLine(person.Name); // Prints "Collin"
    }
}
```

With methods will only be generated for properties with a setter and fields without the ``readonly`` modifier. If you would like Lombok.NET to also generate With methods for inherited members, use `[With(IncludeInheritedMembers = true)]`. 

## Singletons
#### Supported types: Classes

Apply the ``Singleton`` attribute to a partial class and Lombok.NET will generate all the boilerplate code required for making your class a thread-safe, lazy singleton. It will create a property called `Instance` in order to access the singleton's instance. Note that the type needs to have a parameterless constructor.\
**Example:**
```c#
[Singleton]
public partial class PersonRepository {
}

public class MyClass {
    public MyClass() {
        var personRepository = PersonRepository.Instance;
    }
}
```

## Lazy
#### Supported types: Classes, Structs

Apply the ``Lazy`` attribute to a partial class or struct and Lombok.NET will generate a `Lazy<T>` property which can be used to create an instance of the object lazily. Note that the type needs to have a parameterless constructor.
**Example:**
```c#
[Lazy]
public partial class HeavyInitialization {
    private HeavyInitialization() {
        Thread.Sleep(1000);
    }
}

public class Program {
    public Program() {
        var lazy = HeavyInitialization.Lazy;
        if(Random.Shared.Next() == 2) {
            var value = lazy.Value;
            // do something with value
        }
    }
}
```

## ToString
#### Supported types: Classes, Structs, Enums

To generate a descriptive `ToString` method to your type, make it partial and add the `[ToString]` attribute to it. By default, it will include private fields in the `ToString` method, but this is customizable in the attribute's constructor.

```c#
[ToString]
public partial class Person {
    private string _name;
    private int _age;
}
```
When applying this attribute to an enum, Lombok.NET will create an extension class with a `ToText` method. This is due to the fact that enums can't be partial, thus an extension method is needed and the extension method will not be found if it is called `ToString`.

If you have sensitive data in your objects which should not be contained in the `ToString` method, you can apply the `[Masked]` attribute to the property or field containing sensitive data. This will cause the value to be replaced by four asterisks (****) in the `ToString` method.   

## Properties
#### Supported types: Classes, Structs

Generating properties from fields while using them as backing fields is possible using the `[Property]` attribute. Example:
```c#
public partial class MyViewModel {
    
    [Property]
    private int _result;
}
```
This will create the following property:

```c#
public int Result {
    get => _result;
    set => _result = value;
}
```

## Property change pattern
#### Supported types: Classes

All of the boilerplate code surrounding `ÌNotifyPropertyChanged/ÌNotifyPropertyChanging` can be generated using a conjunction of the `[NotifyPropertyChanged]`/`[NotifyPropertyChanging]` and the `[Property]` attributes.\
The `[NotifyPropertyChanged]` attribute will implement the `INotifyPropertyChanged` interface and the `PropertyChanged` event. It will also create a method called `SetFieldAndRaisePropertyChanged` which sets a backing field and raises the event. The event as well as the method can be used in your ViewModels to implement desired behavior.\
If you would like to take it a step further, you can also use the `[Property]` attribute on backing fields while passing the `PropertyChangeType` parameter to generate properties off of backing fields which will include the raising of the specific event in their setters. Here's an example:

```c#
[NotifyPropertyChanged]
public partial class CustomViewModel {

    private int _result;
    
    public int Result {
        get => _result;
        set => SetFieldAndRaisePropertyChanged(out _result, value);
    }
    
    // -- OR --
    
    [Property(PropertyChangeType = PropertyChangeType.PropertyChanged)]
    private int _result;
}

public class Program {

    public static void Main() {
        var vm = new CustomViewModel();
        vm.PropertyChanged += (sender, args) => Console.WriteLine("A property was changed");
        
        vm.Result = 42;
    }
}
```
If you are using the [ReactiveUI](https://www.reactiveui.net/) library (e.g. when using Avalonia), you can also specify the ``PropertyChangeType.ReactivePropertyChange`` to leverage ReactiveUI's property change handling. 

To be able to generate the properties with the property change-raising behavior, the class must have the `[NotifyPropertyChanged]` or `[NotifyPropertyChanging]` (depending on desired behavior) attribute placed above it.

## Serialization
#### Supported types: Classes, Structs
To be able to perform binary serialization and deserialization on a type, apply the `[Serialization]` attribute.
This will generate the following methods:
- `void Serialize(string path)`
- `Task SerializeAsync(string path, CancellationToken cancellationToken)`
- `void Deserialize(string path)`
- `Task DeserializeAsync(string path)`

If deserialization functionality is not needed, use `[Serialization(IncludeDeserialization = false)]`. Similarly, to serialize properties instead of fields, use `[Serialization(MemberType = MemberType.Property)]`.

Lombok.NET will serialize the object including its inherited members. Serialization is only supported for the following data types:
- `short` (`Int16`)
- `int` (`Int32`)
- `long` (`Int64`)
- `ushort` (`UInt16`)
- `uint` (`UInt32`)
- `ulong` (`UInt64`)
- `byte` (`Byte`)
- `sbyte` (`SByte`)
- `float` (`Single`)
- `double` (`Double`)
- `decimal` (`Decimal`)
- `string` (`String`)
- `char` (`Char`)
- `bool` (`Boolean`)

## Async overloads
#### Supported types: Abstract Classes, Interfaces, Methods
If you want to have ``async`` overloads for every method in your interface, you can add the `[AsyncOverloads]` attribute to it. This also works for abstract classes:
```c#
[AsyncOverloads]
public partial interface IRepository<T> {
    T GetById(int id);
    
    void Save(T entity);
}
```

This will add the following methods to your interface:
```c#
Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
Task SaveAsync(T entity, CancellationToken cancellationToken = default);
```
For abstract classes, it will do the same for every abstract method. The inheriting class will be forced to implement the async versions as well. This may also be achieved by using the [[Async]](#async-methods) attribute.

### Async methods
If you would like to create a simple ``async`` version of your method, you can add the `[Async]` attribute to it:
```c#
public partial class MyViewModel {

    [Async]
    public int Square(int i) {
        return i * i;
    }
}
```
This will add the following method:
```c#
public Task<int> SquareAsync(int i) => Task.FromResult(Square(i));
```

This works for classes and structs, however it must be ``partial``.

## Freezable pattern
#### Supported types: Classes and structs
The `[Freezable]` attribute can be used to generate the freezable pattern for types. For example:
```csharp
[Freezable]
partial class Person
{
	[Freezable]
	private string _name;

	private int _age;
}
```
This would generate the methods `Freeze()`, `Unfreeze()`, `TryFreeze()`, and `TryUnfreeze()` and a property to check the freeze status, `IsFrozen`, as well as the property `Name` for the `_name` field.
When trying to set the `Name` property, the setter will check if the type is currently frozen and throw an `InvalidOperationException` if this is the case.\
The attribute must be set on both the fields which should be aware of the type's freeze status as well as the type itself. Readonly fields will be ignored.\
If an instance should not be able to be unfrozen, it is possible to specify `[Freezable(IsUnfreezable = false)]` on the type.

## Decorator Pattern
#### Supported types: Abstract Classes, Interfaces

Lombok.NET also provides an option to generate the boilerplate code when it comes to the decorator pattern. Simply apply the `Decorator` attribute to an abstract class or an interface and let the Source Generator do the rest.
```c#
[Decorator]
public interface IVehicle {
    void Drive();
    int GetNumberOfWheels();
} 
```
This will add the following class to your namespace:
```c#
public class VehicleDecorator {

    private readonly IVehicle _vehicle;
    
    public VehicleDecorator(IVehicle vehicle) {
        _vehicle = vehicle;
    }
    
    public virtual void Drive() {
        _vehicle.Drive();
    }
    
    public virtual int GetNumberOfWheels() {
        return _vehicle.GetNumberOfWheels();
    }
} 
```

Please let me know if there is any other functionality you would like to see in this library. I am happy to add more features.