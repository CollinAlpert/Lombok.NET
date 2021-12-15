# Lombok.NET
This library is to .NET what Lombok is to Java. 
It generates constructors and other fun stuff using Source Generators for those classes you specify special attributes for. Check out the examples for more info.

### Prerequisites
* .NET Standard 2.1

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

## Usage

### Demo
This demonstrates the generating of the `With` pattern. Simply apply an attribute and the library will do the rest. Remember you are not bound to using fields, but can also use properties and supply the appropriate `MemberType` value to the attribute's constructor.

![LombokNetDemo](https://user-images.githubusercontent.com/14217185/140986601-83424d22-57a5-43cb-a491-9234036d245c.gif)

### Constructors
```c#
[AllArgsConstructor]
public partial class Person {
    private string _name;
    private int _age;
}
```

By supplying the `AllArgsConstructor` attribute and making the class `partial`, you allow the Source Generator to create a constructor for it containing all of the classes private fields.\
If you wish to modify this behavior and would instead like to have a constructor generated off of public properties, you can specify this in the attribute's constructor, e.g.:
```c#
[AllArgsConstructor(MemberType.Property, AccessType.Public)]
public partial class Person {
    public string Name { get; set; }
    public int Age { get; set; }
}
```
The default is `Field` for the `MemberType` and `Private` for the `AccessType`.\
It is crucial to make the class `partial`, otherwise the Source Generator will not be able to generate a constructor and will throw an exception.

If you only wish to have a constructor generated containing the required fields or properties, Lombok.NET offers the `RequiredArgsConstructor` attribute. Fields are required if they are `readonly`, properties are required if they don't have a `set` accessor.\
There is also a `NoArgsConstructor` attribute which generates an empty constructor.

### With Methods
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

### Decorator Pattern
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
* Generator which generates the ToString method
* Generator which provides a static factory method.
* Generator which generates immutable ``With`` methods
