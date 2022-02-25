using System.Net;
using Xunit;

namespace Lombok.NET.Test;

public class WithTest
{
	[Fact]
	public void Test()
	{
		var p = new TestPerson();
		p = p.WithName("George").WithAge(46).WithId(2);

		Assert.Equal("George", p.Name);
		Assert.Equal(46, p.Age);
		Assert.Equal(2, p.Id);
	}
	
	[Fact]
	public void Test2()
	{
		var p = new TestPerson2();
		p = p.WithName("Peter").WithAge(32).WithId(99);

		Assert.Equal("Peter", p.GetName());
		Assert.Equal(32, p.GetAge());
		Assert.Equal(99, p.GetId());
	}
	
	[Fact]
	public void Test3()
	{
		var p = new TestPerson3();
		p = p.WithName("Steve").WithS(HttpStatusCode.Accepted).WithId(1);

		Assert.Equal("Steve", p.GetName());
		Assert.Equal(HttpStatusCode.Accepted, p.GetStatusCode());
		Assert.Equal(1, p.GetId());
	}
}

[With(MemberType.Property)]
partial class TestPerson
{
	public int Id { get; set; }
	
	public string Name { get; set; }

	public int Age { get; set; }
}

[With]
partial class TestPerson2
{
	private int _id;

	private string _name;

	private int _age;

	public int GetId() => _id;
	public string GetName() => _name;
	public int GetAge() => _age;
}

[With]
partial class TestPerson3
{
	private int id;

	public string name;

	private HttpStatusCode s;

	public int GetId() => id;
	public string GetName() => name;
	public HttpStatusCode GetStatusCode() => s;
}