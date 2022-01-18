using Xunit;

namespace Lombok.NET.Test;

public class ReservedKeywordTest
{
	[Fact]
	public void Test()
	{
		var r = new ReservedKeywordPerson(2, "", true, ' ');

		Assert.NotNull(r);
		Assert.Equal(2, r.Class);
		Assert.Equal(string.Empty, r.Abstract);
		Assert.True(r.Int);
		Assert.Equal(' ', r.Void);
	}
}

[AllArgsConstructor]
partial class ReservedKeywordPerson
{
	[Property]
	private int _class;

	[Property]
	private string _abstract;

	[Property]
	private bool _int;

	[Property]
	private char _void;
}