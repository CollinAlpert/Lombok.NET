using System.Net;
using Xunit;

namespace Lombok.NET.Test;

public class AsyncOverloadsTest
{
	[Fact]
	public async Task TestInterface()
	{
		IAsyncOverloadInterface i = new AsyncOverloadImplementation();
		await i.RunAsync(2).ConfigureAwait(false);

		Assert.True(await i.IsValidAsync(HttpStatusCode.Accepted));
	}
	
	[Fact]
	public async Task TestInterfaceImplementation()
	{
		var i = new AsyncOverloadImplementation();
		await i.RunAsync(2).ConfigureAwait(false);

		Assert.True(await i.IsValidAsync(HttpStatusCode.Accepted));
	}
	
	[Fact]
	public async Task TestAbstractClass()
	{
		var cls = new AsyncOverloadClassImplementation();
		await cls.RunAsync(Guid.NewGuid()).ConfigureAwait(false);
		
		Assert.Equal(default, await cls.GetCurrentDateAsync());
		Assert.Equal(("", 0), await cls.GetValueAsync());
	}
}

[AsyncOverloads]
internal partial interface IAsyncOverloadInterface
{
	void Run(int i);

	bool IsValid(HttpStatusCode statusCode);
}

internal class AsyncOverloadImplementation : IAsyncOverloadInterface
{
	public void Run(int i)
	{
	}
	
	public Task RunAsync(int i, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public bool IsValid(HttpStatusCode _) => true;

	public Task<bool> IsValidAsync(HttpStatusCode statusCode, CancellationToken cancellationToken = default) => Task.FromResult(IsValid(statusCode));

}

[AsyncOverloads]
internal abstract partial class AsyncOverloadsClass
{
	public abstract void Run(Guid guid);

	public abstract (string, int) GetValue();

	public abstract DateTime GetCurrentDate();
}

internal class AsyncOverloadClassImplementation : AsyncOverloadsClass
{
	public override void Run(Guid guid)
	{
	}
	
	public override Task RunAsync(Guid guid, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public override (string, int) GetValue() => ("", 0);
	
	public override Task<(string, int)> GetValueAsync(CancellationToken cancellationToken = default) => Task.FromResult(GetValue());

	public override DateTime GetCurrentDate()
	{
		return default;
	}

	public override Task<DateTime> GetCurrentDateAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(GetCurrentDate());
	}
}