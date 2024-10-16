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

	[Fact]
	public Task TestWithInit()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With(MemberType = MemberType.Property)]
		                      public partial class Person
		                      {
		                         public string Name { get; init; }
		                         
		                         public int Age { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}

	[Fact]
	public Task TestPropertyWithReservedKeywords()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With(MemberType = MemberType.Property)]
		                      public partial class Person
		                      {
		                         public int Default { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}

	[Fact]
	public Task TestFieldWithReservedKeywords()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With]
		                      public partial class Person
		                      {
		                         private int Default;
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}

	[Fact]
	public Task TestPropertyWithGenerics()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With(MemberType = MemberType.Property)]
		                      public partial class MyClass<T>
		                      {
		                         public T MyProperty { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}

	[Fact]
	public Task TestFieldWithGenerics()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [With]
		                      public partial class MyClass<T>
		                      {
		                         private T _myField;
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}

	[Fact]
	public Task TestWithInheritedProperties()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      public class BaseClass
		                      {
		                          public int Id { get; set; }
		                      }
		                      
		                      [With(MemberType = MemberType.Property, IncludeInheritedMembers = true)]
		                      public partial class InheritingClass : BaseClass
		                      {
		                          public string Name { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<WithMethodsGenerator>(source);
	}
}