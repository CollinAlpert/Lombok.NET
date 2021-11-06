using Lombok.NET;
using Test;

class Program {
	public static void Main() {
		var person = new Person("Steve", 22);
		person = person.WithName("Collin").WithAge(22);
	}
}

namespace Test
{
	[AllArgsConstructor]
	[With]
	public partial class Person {
		private string _name;
		private int _age;
	}
}