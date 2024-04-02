using Lombok.NET.MethodGenerators;

namespace Lombok.NET.Test;

public sealed class FreezableTest
{
	[Fact]
	public Task Name_WhenNotFrozen_NameIsUpdated()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [Freezable]
		                      partial class Person
		                      {
		                      	  [Freezable]
		                      	  private string _name;
		                      
		                      	  private int _age;
		                      }
		                      """;

		return TestHelper.Verify<FreezablePatternGenerator>(source);
	}
}