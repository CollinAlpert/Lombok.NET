using Lombok.NET.MethodGenerators;

namespace Lombok.NET.Test;

public class AsyncTest
{
	[Fact]
	public Task Test()
	{
		const string source = """
		                      using System;
		                      using System.Net;
		                      using Lombok.NET;

		                      namespace Test;

		                      internal partial class MyAsyncViewModel
		                      {
		                      	  [Async]
		                      	  public void Run(int i)
		                      	  {
		                      	  	  Console.Write(1);
		                      	  }
		                      	
		                      	  [Async]
		                      	  public bool IsValid() => true;
		                      
		                      	  [Async]
		                      	  public int GetValue(HttpStatusCode statusCode, int i)
		                      	  {
		                      		  return i;
		                      	  }
		                      }
		                      """;

		return TestHelper.Verify<AsyncGenerator>(source);
	}
}