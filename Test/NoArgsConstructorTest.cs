using Lombok.NET;

namespace Test
{
	public class NoArgsConstructorTest
	{
		public NoArgsConstructorTest()
		{
			var p = new NoArgsPerson();
		}
	}

	
	[NoArgsConstructor]
	partial class NoArgsPerson
	{
		private string _name;
		private string _age;

		public NoArgsPerson(string name, string age)
		{
			_name = name;
			_age = age;
		}
	}
}