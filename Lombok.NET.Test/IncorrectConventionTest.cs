namespace Lombok.NET.Test;

public class IncorrectConventionTest
{
	public IncorrectConventionTest()
	{
		var model = new IncorrectConventionModel("Steve");
	}
}

[AllArgsConstructor(MemberType.Property, AccessTypes.Public)]
public partial class IncorrectConventionModel
{
	public string name { get; set; }
}