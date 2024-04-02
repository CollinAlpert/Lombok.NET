using Lombok.NET.PropertyGenerators;

namespace Lombok.NET.Test;

public sealed class EnumValuesTest
{
	[Fact]
	public Task Test()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [EnumValues]
		                      public enum MyEnum
		                      {
		                      	  One, Two, Three
		                      }
		                      """;

		return TestHelper.Verify<EnumValuesGenerator>(source);
	}
}