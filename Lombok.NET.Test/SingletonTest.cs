using Xunit;

namespace Lombok.NET.Test;

public class SingletonTest
{
	[Fact]
	public void TestPersonRepository()
	{
		var personRepository = PersonRepository.Instance;
		var data = personRepository.GetNames();

		Assert.Contains(data, s => s == "Peter");
	}

	[Fact]
	public void TestBillingModule()
	{
		var personRepository = BillingModule.Instance;
		var data = personRepository.GetPrices();

		Assert.Contains(data, p => p < 10);
	}
}

[Singleton]
partial class PersonRepository
{
	public IEnumerable<string> GetNames()
	{
		yield return "Steve";
		yield return "Peter";
	}
}

[Singleton]
partial class BillingModule
{
	public IEnumerable<int> GetPrices()
	{
		yield return 2;
		yield return 5;
	}
}