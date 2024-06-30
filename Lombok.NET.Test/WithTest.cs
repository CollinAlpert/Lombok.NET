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
	
	[Fact]
	public Task TestWithInheritedMembers()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;

		                      namespace Test;

		                      #nullable enable
		                      [With(IncludeInheritedMembers = true)]
		                      partial class Person : BasePerson
		                      {
		                      	  public string name = default!;
		                      	  public string? remark;
		                      	  private HttpStatusCode s;
		                      }
		                      
		                      class BasePerson
		                      {
		                          private int id;
		                          private int? referencedId;
		                          private Data data = default!;
		                          private Data? data2;
		                      }
		                      
		                      class Data
		                      {
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}

	[Fact]
	public Task TestMixedFieldsAndProperties()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With]
		                      partial class Person
		                      {
		                      	  private int id;
		                      	  
		                      	  public int Age { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}
}