using Xunit;

namespace Lombok.NET.Test;

public class PropertyTest
{
	public PropertyTest()
	{
		var person = new PropertyPerson("Collin", 22);
		
		Assert.Equal("Collin", person.Name);
		Assert.Equal(22, person.Age);
	}
}

[AllArgsConstructor]
partial class PropertyPerson
{
	[Property]
	private string _name;

	[Property]
	private readonly int _age;
}