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
		private readonly string _name;
		private int _age;
		
		public NoArgsPerson(string name, int age)
		{
			_name = name;
			_age = age;
		}
	}
}