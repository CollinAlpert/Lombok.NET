# Lombok.NET
This library is to .NET what Lombok is to Java. 
It generates constructors and other fun stuff using Source Generators for those classes you specify special attributes for. Check out the examples for more info.

### Prerequisites
* At least .NET 5

### Disclaimer
This project is in its early stages (< v1.0.0) so there might be some breaking changes along the way, depending on community feedback.\
I will continuously add features and am happy to respond to feature requests. Just file an issue and I'll get to it as soon as possible.

### Installation
You can install Lombok.NET either via [NuGet](https://www.nuget.org/packages/Lombok.NET)
```
Install-Package Lombok.NET
```
Or via the .NET Core command-line interface:
```
dotnet add package Lombok.NET
```

## Features

- [Constructors](#constructors)
- ["With" methods](#with-methods)
- [Singletons](#singletons)
- [INotifyPropertyChanged/INotifyPropertyChanging](#property-change-pattern)
- [Async overloads](#async-overloads)
- [ToString](#tostring)
- [Decorator pattern](#decorator-pattern)

## Usage

### Demo
This demonstrates the generating of the `With` pattern. Simply apply an attribute and the library will do the rest. Remember you are not bound to using fields, but can also use properties and supply the appropriate `MemberType` value to the attribute's constructor.

![LombokNetDemo](https://user-images.githubusercontent.com/14217185/140986601-83424d22-57a5-43cb-a491-9234036d245c.gif)

### Constructors
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
[AllArgsConstructor(MemberType.Property, AccessType.Public)]
public partial class Person {
    public string Name { get; set; }
    public int Age { get; set; }
}
```
The default is `Field` for the `MemberType` and `Private` for the `AccessType`.\
It is crucial to make the type `partial`, otherwise the Source Generator will not be able to generate a constructor and will throw an exception.

If you only wish to have a constructor generated containing the required fields or properties, Lombok.NET offers the `RequiredArgsConstructor` attribute. Fields are required if they are `readonly`, properties are required if they don't have a `set` accessor.\
There is also a `NoArgsConstructor` attribute which generates an empty constructor.

### With Methods
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

With methods will only be generated for properties with a setter and fields without the ``readonly`` modifier.

### Singletons
#### Supported types: Classes

Apply the ``Singleton`` attribute to any partial class and Lombok.NET will generate all the boilerplate code required for making your class a thread-safe, lazy singleton. It will create a property called `Instance` in order to access the singleton's instance.\
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

### ToString
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

### Properties
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

### Property change pattern
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
    
    [Property(PropertyChangeType.PropertyChanged)]
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

To be able to generate the properties with the property change-raising behavior, the class must have the `[NotifyPropertyChanged]` or `[NotifyPropertyChanging]` (depending on desired behavior) attribute placed above it.

### Async overloads
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
Task<T> GetByIdAsync(int id);
Task SaveAsync(T entity);
```
For abstract classes, it will do the same for every abstract method. The inheriting class will be forced to implement the async versions as well. This may also be achieved by using the [[Async]](#async-methods) attribute.

#### Async methods
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


### Decorator Pattern
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

Planned:
* Switch to Incremental Generators
* NRT
* Generator which generates immutable ``With`` methods
* [Equals] and [HashCode] generators
