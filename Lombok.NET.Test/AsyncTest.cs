using Xunit;

namespace Lombok.NET.Test;

public class AsyncTest
{
	[Fact]
	public async Task Test()
	{
		var vm = new MyAsyncViewModel();

		await vm.RunAsync(2).ConfigureAwait(false);

		Assert.Equal(5, await vm.GetValueAsync(Guid.NewGuid(), 5));
		Assert.Equal(1337, await vm.GetValueAsync(Guid.NewGuid(), 1337));
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
	public int GetValue(Guid guid, int i)
	{
		return i;
	}
}