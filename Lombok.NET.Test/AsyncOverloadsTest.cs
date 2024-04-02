using Lombok.NET.MethodGenerators;

namespace Lombok.NET.Test;

public class AsyncOverloadsTest
{
	[Fact]
	public Task TestInterface()
	{
		const string source = """
		                      using System;
		                      using System.Net;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [AsyncOverloads]
		                      internal partial interface IAsyncOverloadInterface
		                      {
		                      	  void Run(int i);
		                      
		                      	  bool IsValid(HttpStatusCode statusCode);
		                      }
		                      """;

		return TestHelper.Verify<AsyncOverloadsGenerator>(source);
	}
	
	[Fact]
	public Task TestAbstractClass()
	{
		const string source = """
		                      using System;
		                      using Lombok.NET;

		                      namespace Test;

		                      [AsyncOverloads]
		                      internal abstract partial class AsyncOverloadsClass
		                      {
		                      	  public abstract void Run(Guid guid);
		                      
		                      	  public abstract (string, int) GetValue();
		                      
		                      	  public abstract DateTime GetCurrentDate();
		                      }
		                      """;

		return TestHelper.Verify<AsyncOverloadsGenerator>(source);
	}
}