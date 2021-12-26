using System;
using Lombok.NET;

namespace Test;

public class ToStringTest
{
	public ToStringTest()
	{
		var person = new ToStringPerson("Collin", 22);
		Console.WriteLine(person.ToString());
	}
}

[ToString]
[AllArgsConstructor]
public partial class ToStringPerson
{
	private string _name;
	private int _age;
}


[ToString(MemberType.Property, AccessTypes.Public)]
public partial class ToStringPerson2
{
	public string Name { get; set; }

	public int Age { get; set; }
}