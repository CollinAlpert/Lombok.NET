using Lombok.NET.ConstructorGenerators;

namespace Lombok.NET.Test;

public class AllArgsConstructorTest
{
	[Fact]
	public Task TestWithFullyQualifiedName()
	{
		const string source = """
		                namespace Test;

		                [Lombok.NET.AllArgsConstructor]
		                partial class Person
		                {
		                	private string _name;
		                	private int _age;
		                }
		                """;

		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithProtectedFields()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [AllArgsConstructor(AccessTypes = AccessTypes.Protected)]
		                      partial class Person
		                      {
		                      	  protected string _name;
		                      	  protected int _age;
		                      }
		                      """;

		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithPrivateProperties()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor(MemberType = MemberType.Property)]
		                      partial class Person
		                      {
		                      	  private string Name { get; set; }
		                      	  private int Age { get; set; }
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithPublicProperties()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial class Person
		                      {
		                      	  public string Name { get; set; }
		                      	  public int Age { get; set; }
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithEmptyClass()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor]
		                      partial class Person
		                      {
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source, true);
	}

	[Fact]
	public Task TestWithGenerics()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial class AllArgsValueWrapper<T>
		                      {
		                      	  public T Value { get; set; }
		                      	  public string Text { get; set; }
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}
	
	[Fact]
	public Task TestStructWithPrivateFields()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor]
		                      partial struct Person
		                      {
		                      	  private string _name;
		                      	  private int _age;
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestStructWithPrivateProperties()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor(MemberType = MemberType.Property)]
		                      partial struct Person
		                      {
		                      	  private string Name { get; set; }
		                      	  private int Age { get; set; }
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestStructWithPublicProperties()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial struct Person
		                      {
		                      	  public string Name { get; set; }
		                      	  public int Age { get; set; }
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestEmptyStruct()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor]
		                      partial struct Person
		                      {
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source, true);
	}

	[Fact]
	public Task TestStructWithGenerics()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;

		                      [AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial class AllArgsValueWrapperStruct<T>
		                      {
		                      	  public T Value { get; set; }
		                      	  public int Index { get; set; }
		                      }
		                      """;
		
		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithConflictingNamespaces()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace API.Controllers.v1
		                      {
		                      	  [AllArgsConstructor]
		                      	  public partial class MyController
		                      	  {
		                      	  	  private string _value;
		                      	  }
		                      }

		                      namespace API.Controllers.v2
		                      {
		                      	  [AllArgsConstructor]
		                      	  public partial class MyController
		                      	  {
		                      		  private string _value;
		                      	  }
		                      }
		                      """;

		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestIncorrectConvention()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;
		                      
		                      [AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      public partial class IncorrectConventionModel
		                      {
		                      	  public string name { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithReservedKeywords()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;
		                      
		                      [AllArgsConstructor]
		                      partial class ReservedKeywordPerson
		                      {
		                      	  private int _class;
		                      	  private string _abstract;
		                      	  private bool _int;
		                      	  private char _void;
		                      }
		                      """;

		return TestHelper.Verify<AllArgsConstructorGenerator>(source);
	}
}