using Xunit;

namespace Lombok.NET.Test;

public class AllArgsConstructorTest
{
	[Fact]
	public void Test1()
	{
		var person = new AllArgsPerson1("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void Test2()
	{
		var person = new AllArgsPerson2("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void Test3()
	{
		var person = new AllArgsPerson3("Robert", 80);

		Assert.Equal("Robert", person.GetName());
		Assert.Equal(80, person.GetAge());
	}

	[Fact]
	public void Test4()
	{
		var person = new AllArgsPerson4("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void Test5()
	{
		var person = new AllArgsPerson5();

		Assert.NotNull(person);
	}
}

[AllArgsConstructor]
partial class AllArgsPerson1
{
	[Property]
	private string _name;

	[Property]
	private int _age;
}

[AllArgsConstructor(AccessTypes.Protected)]
partial class AllArgsPerson2
{
	[Property]
	protected string _name;

	[Property]
	protected int _age;
}

[AllArgsConstructor(MemberType.Property)]
partial class AllArgsPerson3
{
	private string Name { get; set; }
	private int Age { get; set; }

	public string GetName() => Name;
	public int GetAge() => Age;
}

[AllArgsConstructor(MemberType.Property, AccessTypes.Public)]
partial class AllArgsPerson4
{
	public string Name { get; set; }
	public int Age { get; set; }
}

[AllArgsConstructor]
partial class AllArgsPerson5
{
}