using Lombok.NET.ConstructorGenerators;

namespace Lombok.NET.Test;

public class NoArgsConstructorTest
{
	[Fact]
	public Task Test()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [NoArgsConstructor]
		                      partial class NoArgsPerson
		                      {
		                      	  private readonly string _name;
		                      	  private int _age;
		                      
		                      	  public NoArgsPerson(string name, int age)
		                      	  {
		                      		  _name = name;
		                      		  _age = age;
		                      	  }
		                      }
		                      """;

		return TestHelper.Verify<NoArgsConstructorGenerator>(source);
	}
}