using System;
using Lombok.NET;

namespace Test
{
	public class WithTest
	{
		public WithTest()
		{
			var person = new TestPerson();
			person = person.WithName("Collin").WithAge(22);

			Console.WriteLine(person.Name);
		}
	}
	
	[With(MemberType.Property)]
	partial class TestPerson
	{
		public string Name { get; set; }
		
		public int Age { get; set; } 
	}
}