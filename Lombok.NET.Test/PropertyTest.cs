using System.ComponentModel.DataAnnotations;
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

partial class PropertyPersonWithComments
{
	[Property]
	/*
	 * The person's name.
	 */
	string _name;
	
	[Property]
	/// <summary>
	/// The person's height.
	/// </summary>
	int _height;

	[Property]
	/// <summary>
	/// The person's age.
	/// </summary>
	private readonly int _age;

	// The person's status code.
	[Property]
	private readonly HttpStatusCode _statusCode;
}

partial class PropertyPersonWithValidationAttributes
{
	[Property]
	[System.ComponentModel.DataAnnotations.MaxLength(20)]
	private string _name;
	
	[Property]
	[EmailAddress]
	private string _email;
}