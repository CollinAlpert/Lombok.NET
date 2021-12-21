using Lombok.NET;

namespace Test;

public class ReservedKeywordTest
{
	public ReservedKeywordTest()
	{
		var r = new ReservedKeywordPerson(2, "", true, ' ');
	}
}

[AllArgsConstructor]
public partial class ReservedKeywordPerson
{
	private int _class;
	private string _abstract;
	private bool _int;
	private char _void;
}

