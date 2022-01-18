using Xunit;

namespace Lombok.NET.Test;

public class PropertyTest
{
	[Fact]
	public void ClassTest()
	{
		var person = new PropertyPerson("Collin", 22);

		Assert.Equal("Collin", person.Name);
		Assert.Equal(22, person.Age);
	}
	
	[Fact]
	public void StructTest()
	{
		var person = new PropertyPersonStruct("Collin", 22);

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

[AllArgsConstructor]
partial struct PropertyPersonStruct
{
	[Property]
	private string _name;

	[Property]
	private readonly int _age;
}