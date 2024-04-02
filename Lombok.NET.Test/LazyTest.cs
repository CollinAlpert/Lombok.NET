using Lombok.NET.PropertyGenerators;

namespace Lombok.NET.Test;

public class LazyTest
{
	[Fact]
	public Task Test()
	{
		const string source = """
		                      using System.Threading;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [Lazy]
		                      partial class HeavyInitialization {
		                      	  private HeavyInitialization() {
		                              Thread.Sleep(1000);
		                      	  }
		                      
		                      	  public int GetValue()
		                      	  {
		                      		  return 2;
		                      	  }
		                      }
		                      """;

		return TestHelper.Verify<LazyGenerator>(source);
	}
}