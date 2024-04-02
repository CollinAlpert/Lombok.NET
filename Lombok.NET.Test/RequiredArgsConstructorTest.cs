using Lombok.NET.ConstructorGenerators;

namespace Lombok.NET.Test;

public class RequiredArgsConstructorTest
{
	[Fact]
	public Task TestWithPrivateFields()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor]
		                      partial class RequiredArgsPerson
		                      {
		                      	  private readonly string _name;
		                      	  private int _age;
		                      }
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source);
	}
	
	[Fact]
	public Task TestWithPrivateAndProtectedFields()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor(AccessTypes = AccessTypes.Protected | AccessTypes.Private)]
		                      partial class RequiredArgsPerson
		                      {
		                      	  protected readonly string _name;
		                      	  protected int _age;
		                      	  private readonly double _height;
		                      }
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source);
	}
	
	[Fact]
	public Task TestWithPrivateProperties()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor(MemberType = MemberType.Property)]
		                      partial class RequiredArgsPerson
		                      {
		                      	  private string Name { get; }
		                      	  private int Age { get; set; }
		                      	  private HttpStatusCode StatusCode { get; }
		                      }
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithPublicProperties()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial class RequiredArgsPerson
		                      {
		                      	  public string Name { get; }
		                      	  public int Age { get; set; }
		                      }
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithEmptyInternalClass()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor]
		                      partial class RequiredArgsPerson;
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source, true);
	}

	[Fact]
	public Task TestWithEmptyPublicClass()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor]
		                      public partial class RequiredArgsPerson;
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source, true);
	}

	[Fact]
	public Task TestWithEventHandler()
	{
		const string source = """
		                      using System;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor]
		                      public partial class RequiredArgsPerson
		                      {
		                      	private readonly EventHandler @event;
		                      }
		                      """;
		
		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithReservedKeyword()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor]
		                      public partial class RequiredArgsPerson
		                      {
		                      	  private readonly string test;
		                      	  private readonly string Test;
		                      	  private readonly int value;
		                      	  private readonly int _value;
		                      	  private readonly string @string;
		                      }
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source);
	}

	[Fact]
	public Task TestWithGenerics()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [RequiredArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
		                      partial class RequiredArgsValueWrapper<T>
		                      {
		                      	  public T Value { get; }
		                      	  public string Text { get; }
		                      }
		                      """;

		return TestHelper.Verify<RequiredArgsConstructorGenerator>(source);
	}
}