using Xunit;

namespace Lombok.NET.Test;

public class ToStringTest
{
	[Fact]
	public void Test1()
	{
		var p = new ToStringPerson("Peter", 85);

		Assert.NotNull(p);
		Assert.Equal("ToStringPerson: _name=Peter; _age=85", p.ToString());
	}

	[Fact]
	public void Test2()
	{
		var p = new ToStringPerson2
		{
			Name = "Peter",
			Age = 85
		};

		Assert.Equal("ToStringPerson2: Name=Peter; Age=85", p.ToString());
	}
}

[ToString]
[AllArgsConstructor]
partial class ToStringPerson
{
	private string _name;
	private int _age;
}

[ToString(MemberType.Property, AccessTypes.Public)]
partial class ToStringPerson2
{
	public string Name { get; set; }

	public int Age { get; set; }
}