using Lombok.NET;

namespace Test;

[Singleton]
public partial class PersonRepository
{
	
}


[Singleton]
public partial class BillingModule
{
	
}

public class SingletonTest
{
	public SingletonTest()
	{
		var personRepository = PersonRepository.Instance;
		var billingModule = BillingModule.Instance;
	}
}