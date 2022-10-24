using Xunit;

namespace Lombok.NET.Test;

public class AllArgsConstructorTest
{
	[Fact]
	public void ClassTest1()
	{
		var person = new AllArgsPerson1("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void ClassTest2()
	{
		var person = new AllArgsPerson2("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void ClassTest3()
	{
		var person = new AllArgsPerson3("Robert", 80);

		Assert.Equal("Robert", person.GetName());
		Assert.Equal(80, person.GetAge());
	}

	[Fact]
	public void ClassTest4()
	{
		var person = new AllArgsPerson4("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void ClassTest5()
	{
		var person = new AllArgsPerson5();

		Assert.NotNull(person);
	}

	[Fact]
	public void ClassTest6()
	{
		var person = new AllArgsValueWrapper<int>(2, "Two");

		Assert.Equal(2, person.Value);
		Assert.Equal("Two", person.Text);
	}
	
	// -- STRUCTS --
	
	[Fact]
	public void StructTest1()
	{
		var person = new AllArgsStructPerson1("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void StructTest2()
	{
		var person = new AllArgsStructPerson2("Robert", 80);

		Assert.Equal("Robert", person.GetName());
		Assert.Equal(80, person.GetAge());
	}

	[Fact]
	public void StructTest3()
	{
		var person = new AllArgsStructPerson3("Robert", 80);

		Assert.Equal("Robert", person.Name);
		Assert.Equal(80, person.Age);
	}

	[Fact]
	public void StructTest4()
	{
		var person = new AllArgsStructPerson4();

		Assert.Equal(default, person);
	}

	[Fact]
	public void StructTest5()
	{
		var person = new AllArgsValueWrapperStruct<int>(2, 1);

		Assert.Equal(2, person.Value);
		Assert.Equal(1, person.Index);
	}
}

[Lombok.NET.AllArgsConstructor]
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

[AllArgsConstructor(MemberType.Property, AccessTypes.Public)]
partial class AllArgsValueWrapper<T>
{
	public T Value { get; set; }
	public string Text { get; set; }
}

[AllArgsConstructor]
partial struct AllArgsStructPerson1
{
	[Property]
	private string _name;

	[Property]
	private int _age;
}

[AllArgsConstructor(MemberType.Property)]
partial struct AllArgsStructPerson2
{
	private string Name { get; set; }
	private int Age { get; set; }

	public string GetName() => Name;
	public int GetAge() => Age;
}

[AllArgsConstructor(MemberType.Property, AccessTypes.Public)]
partial struct AllArgsStructPerson3
{
	public string Name { get; set; }
	public int Age { get; set; }
}

[AllArgsConstructor]
partial struct AllArgsStructPerson4
{
}

[AllArgsConstructor(MemberType.Property, AccessTypes.Public)]
partial class AllArgsValueWrapperStruct<T>
{
	public T Value { get; set; }
	public int Index { get; set; }
}