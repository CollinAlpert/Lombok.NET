using Lombok.NET.MethodGenerators;

namespace Lombok.NET.Test;

public class WithTest
{
	[Fact]
	public Task TestWithProperties()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With(MemberType = MemberType.Property)]
		                      partial class Person
		                      {
		                      	  public int Id { get; set; }
		                      	
		                      	  public string Name { get; set; }
		                      
		                      	  public int Age { get; set; }
		                      
		                      	  public static string Car { get; set; } = "Volvo";
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}
	
	[Fact]
	public Task TestWithFields()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With]
		                      partial class Person
		                      {
		                      	  private static int Value = 1;
		                      	  private int _id;
		                      	  private string _name;
		                      	  private int _age;
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}
	
	[Fact]
	public Task TestWithMixedAccessibilityFields()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;

		                      namespace Test;

		                      [With]
		                      partial class Person
		                      {
		                      	  private int id;
		                      	  public string name;
		                      	  private HttpStatusCode s;
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}
}