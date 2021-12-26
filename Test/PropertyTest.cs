using System;
using Lombok.NET;

namespace Test;

public class PropertyTest
{
	public PropertyTest()
	{
		var person = new PropertyPerson("Collin", 22);
		Console.WriteLine(person.Name);
		Console.WriteLine(person.Age);
	}
}

[AllArgsConstructor]
public partial class PropertyPerson
{
	[Property]
	private string _name;

	[Property]
	private readonly int _age;
}