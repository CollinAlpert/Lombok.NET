using Lombok.NET.PropertyGenerators;

namespace Lombok.NET.Test;

public class SingletonTest
{
	[Fact]
	public Task Test()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      [Singleton]
		                      partial class PersonRepository;
		                      """;

		return TestHelper.Verify<SingletonGenerator>(source);
	}
}