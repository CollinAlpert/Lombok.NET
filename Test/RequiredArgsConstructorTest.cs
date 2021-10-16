using Lombok.NET;

namespace Test
{

	
	public class RequiredArgsConstructorTest
	{
		public RequiredArgsConstructorTest()
		{
			var person1 = new RequiredArgsPerson1("Robert");
			var person2 = new RequiredArgsPerson2("Robert");
			var person3 = new RequiredArgsPerson3("Robert");
			var person4 = new RequiredArgsPerson4("Robert");
			var person5 = new RequiredArgsPerson5();
		}
	}

	[RequiredArgsConstructor]
	partial class RequiredArgsPerson1
	{
		private readonly string _name;
		private int _age;
	}

	[RequiredArgsConstructor(AccessType.Protected)]
	partial class RequiredArgsPerson2
	{
		protected readonly string _name;
		protected int _age;
	}

	[RequiredArgsConstructor(MemberType.Property)]
	partial class RequiredArgsPerson3
	{
		private string Name { get; }
		private int Age { get; set; }
	}

	[RequiredArgsConstructor(MemberType.Property, AccessType.Public)]
	partial class RequiredArgsPerson4
	{
		public string Name { get; }
		public int Age { get; set; }
	}

	[RequiredArgsConstructor]
	partial class RequiredArgsPerson5
	{
	}
}