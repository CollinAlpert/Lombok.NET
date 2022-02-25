using System.Net;
using Xunit;

namespace Lombok.NET.Test;

public class RequiredArgsConstructorTest
{
	[Fact]
	public void Test1()
	{
		var person = new RequiredArgsPerson1("Robert");

		Assert.Equal("Robert", person.Name);
	}

	[Fact]
	public void Test2()
	{
		var person = new RequiredArgsPerson2("Robert", 1.87);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(1.87, person.Height);
	}

	[Fact]
	public void Test3()
	{
		var person = new RequiredArgsPerson3("Robert", HttpStatusCode.Accepted);

		Assert.Equal("Robert", person.GetName());
	}

	[Fact]
	public void Test4()
	{
		var person = new RequiredArgsPerson4("Robert");

		Assert.Equal("Robert", person.Name);
	}

	[Fact]
	public void Test5()
	{
		var person = new RequiredArgsPerson5();

		Assert.NotNull(person);
	}

	[Fact]
	public void Test6()
	{
		var person = new RequiredArgsPerson6();

		Assert.NotNull(person);
	}
}

[RequiredArgsConstructor]
partial class RequiredArgsPerson1
{
	[Property]
	private readonly string _name;

	private int _age;
}

[RequiredArgsConstructor(AccessTypes.Protected | AccessTypes.Private)]
partial class RequiredArgsPerson2
{
	[Property]
	protected readonly string _name;

	protected int _age;

	[Property]
	private readonly double _height;
}

[RequiredArgsConstructor(MemberType.Property)]
partial class RequiredArgsPerson3
{
	private string Name { get; }
	private int Age { get; set; }
	private HttpStatusCode StatusCode { get; }

	public string GetName() => Name;
}

[RequiredArgsConstructor(MemberType.Property, AccessTypes.Public)]
partial class RequiredArgsPerson4
{
	public string Name { get; }
	public int Age { get; set; }
}

[RequiredArgsConstructor]
partial class RequiredArgsPerson5
{
}

[RequiredArgsConstructor]
public partial class RequiredArgsPerson6
{
}