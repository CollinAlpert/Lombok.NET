using System.Net;
using Xunit;

namespace Lombok.NET.Test;

public class AsyncTest
{
	[Fact]
	public async Task Test()
	{
		var vm = new MyAsyncViewModel();

		await vm.RunAsync(2).ConfigureAwait(false);

		Assert.Equal(5, await vm.GetValueAsync(HttpStatusCode.Accepted, 5));
		Assert.Equal(1337, await vm.GetValueAsync(HttpStatusCode.Accepted, 1337));
		Assert.True(await vm.IsValidAsync());
	}
}

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