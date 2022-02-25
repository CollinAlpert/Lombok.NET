using System.Net;
using Xunit;

namespace Lombok.NET.Test;

public class DecoratorTest
{
	[Fact]
	public void CoffeeTest()
	{
		Beverage coffee = new Coffee();
		Beverage coffeeWithMilk = new WithMilkDecorator(new Coffee());

		Assert.Equal(2.0, coffee.GetPrice());
		Assert.Equal(2.5, coffeeWithMilk.GetPrice());
	}

	[Fact]
	public void TeaTest()
	{
		Beverage tea = new Tea();
		Beverage teaWithMilk = new WithMilkDecorator(new Tea());

		Assert.Equal(1.5, tea.GetPrice());
		Assert.Equal(2.0, teaWithMilk.GetPrice());
	}

	[Fact]
	public void VehicleTest()
	{
		IVehicle bike = new Bicycle();
		IVehicle bikeWithTrainingWheels = new TrainingWheelsDecorator(new Bicycle());

		Assert.Equal(2, bike.GetNumberOfWheels());
		Assert.Equal(4, bikeWithTrainingWheels.GetNumberOfWheels());
	}
}

class WithMilkDecorator : BeverageDecorator
{
	public WithMilkDecorator(Beverage beverage) : base(beverage)
	{
	}

	public override double GetPrice()
	{
		return base.GetPrice() + 0.5;
	}
}

class Coffee : Beverage
{
	public override double GetPrice()
	{
		return 2.0;
	}
}

class Tea : Beverage
{
	public override double GetPrice()
	{
		return 1.5;
	}
}

[Decorator]
public abstract class Beverage
{
	public abstract double GetPrice();
}

class TrainingWheelsDecorator : VehicleDecorator
{
	public TrainingWheelsDecorator(IVehicle vehicle) : base(vehicle)
	{
	}

	public override int GetNumberOfWheels()
	{
		return base.GetNumberOfWheels() + 2;
	}
}

class Bicycle : IVehicle
{
	public void Drive()
	{
	}

	public int GetNumberOfWheels() => 2;
	
	public HttpStatusCode GetStatusCode() => HttpStatusCode.Accepted;
}

[Decorator]
interface IVehicle
{
	void Drive();
	int GetNumberOfWheels();

	HttpStatusCode GetStatusCode();
}