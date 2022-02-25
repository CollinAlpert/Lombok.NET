using System.Net;
using Xunit;

namespace Lombok.NET.Test;

public class PropertyTest
{
	[Fact]
	public void ClassTest()
	{
		var person = new PropertyPerson("Collin", 22, HttpStatusCode.Accepted);

		Assert.Equal("Collin", person.Name);
		Assert.Equal(22, person.Age);
		Assert.Equal(HttpStatusCode.Accepted, person.StatusCode);
	}
	
	[Fact]
	public void StructTest()
	{
		var person = new PropertyPersonStruct("Collin", 22, HttpStatusCode.Accepted);

		Assert.Equal("Collin", person.Name);
		Assert.Equal(22, person.Age);
		Assert.Equal(HttpStatusCode.Accepted, person.StatusCode);
	}
}

[AllArgsConstructor]
partial class PropertyPerson
{
	[Property]
	private string _name;

	[Property]
	private readonly int _age;

	[Property]
	private readonly HttpStatusCode _statusCode;
}

[AllArgsConstructor]
partial struct PropertyPersonStruct
{
	[Property]
	private string _name;

	[Property]
	private readonly int _age;
	
	[Property]
	private readonly HttpStatusCode _statusCode;
}