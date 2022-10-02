using Xunit;

namespace Lombok.NET.Test;

public class LazyTest
{
	[Fact]
	public void Test()
	{
		var lazy = HeavyInitialization.Lazy;
		Assert.Equal(2, lazy.Value.GetValue());
	}
}

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