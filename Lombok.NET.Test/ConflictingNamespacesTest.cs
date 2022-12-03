using Lombok.NET;

namespace API.Controllers.v1
{
	[AllArgsConstructor]
	public partial class MyController
	{
		private string _value;
	}
}

namespace API.Controllers.v2
{
	[AllArgsConstructor]
	public partial class MyController
	{
		private string _value;
	}
}