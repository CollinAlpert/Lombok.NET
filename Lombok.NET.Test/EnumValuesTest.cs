using Xunit;

namespace Lombok.NET.Test;

public sealed class EnumValuesTest
{
	[Fact]
	public void Test()
	{
		var values = MyEnumValues.Values;
		Assert.Equal(3, values.Length);
		Assert.Equal(MyEnum.One, values[0]);
		Assert.Equal(MyEnum.Two, values[1]);
		Assert.Equal(MyEnum.Three, values[2]);
	}
}

[EnumValues]
public enum MyEnum
{
	One, Two, Three
}