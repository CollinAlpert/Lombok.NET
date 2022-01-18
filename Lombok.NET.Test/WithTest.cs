using Xunit;

namespace Lombok.NET.Test;

public class WithTest
{
	[Fact]
	public void Test()
	{
		var p = new TestPerson();
		p = p.WithName("George").WithAge(46);

		Assert.Equal("George", p.Name);
		Assert.Equal(46, p.Age);
	}
}

[With(MemberType.Property)]
partial class TestPerson
{
	public string Name { get; set; }

	public int Age { get; set; }
}