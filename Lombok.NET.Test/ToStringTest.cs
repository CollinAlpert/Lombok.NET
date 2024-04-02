using Lombok.NET.MethodGenerators;

namespace Lombok.NET.Test;

public class ToStringTest
{
	[Fact]
	public Task TestClass()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [ToString]
		                      partial class Person
		                      {
		                      	  private string _name;
		                      	  private int _age;
		                      	  
		                      	  [Masked]
		                      	  private string _password;
		                      }
		                      """;

		return TestHelper.Verify<ToStringGenerator>(source);
	}

	[Fact]
	public Task TestClassWithPublicProperties()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [ToString(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial class Person
		                      {
		                      	  public string Name { get; set; }
		                      
		                      	  public int Age { get; set; }
		                      	
		                      	  [Masked]
		                      	  public string Password { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<ToStringGenerator>(source);
	}
	
	[Fact]
	public Task TestStruct()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [ToString]
		                      partial struct Person
		                      {
		                      	  private string _name;
		                      	  private int _age;
		                      }
		                      """;

		return TestHelper.Verify<ToStringGenerator>(source);
	}

	[Fact]
	public Task TestStructWithPublicProperties()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [ToString(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial struct Person
		                      {
		                      	  public string Name { get; set; }
		                      
		                      	  public int Age { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<ToStringGenerator>(source);
	}
	
	[Fact]
	public Task TestEnum()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [ToString]
		                      enum Mood
		                      {
		                      	  Happy,
		                      	  Sad,
		                      	  Mad
		                      }
		                      """;

		return TestHelper.Verify<ToTextGenerator>(source);
	}
}