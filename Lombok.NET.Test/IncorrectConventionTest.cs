namespace Lombok.NET.Test;

public class IncorrectConventionTest
{
	public IncorrectConventionTest()
	{
		var model = new IncorrectConventionModel("Steve");
	}
}

[AllArgsConstructor(MemberType = MemberType.Property, AccessTypes = AccessTypes.Public)]
public partial class IncorrectConventionModel
{
	public string name { get; set; }
}