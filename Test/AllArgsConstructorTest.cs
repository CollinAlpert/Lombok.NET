using Lombok.NET;

namespace Test
{
	public class AllArgsConstructorTest
	{
		public AllArgsConstructorTest()
		{
			var person1 = new AllArgsPerson1("Robert", 80);
			var person2 = new AllArgsPerson2("Robert", 80);
			var person3 = new AllArgsPerson3("Robert", 80);
			var person4 = new AllArgsPerson4("Robert", 80);
			var person5 = new AllArgsPerson5();
		}
	}

	[AllArgsConstructor]
	partial class AllArgsPerson1
	{
		private string _name;
		private int _age;
	}

	[AllArgsConstructor(AccessTypes.Protected)]
	partial class AllArgsPerson2
	{
		protected string _name;
		protected int _age;
	}

	[AllArgsConstructor(MemberType.Property)]
	partial class AllArgsPerson3
	{
		private string Name { get; set; }
		private int Age { get; set; }
	}

	[AllArgsConstructor(MemberType.Property, AccessTypes.Public)]
	partial class AllArgsPerson4
	{
		public string Name { get; set; }
		public int Age { get; set; }
	}

	[AllArgsConstructor]
	partial class AllArgsPerson5
	{
	}
}