using Lombok.NET;

namespace Test
{
	class CoffeeWithMilk : CoffeeDecorator
	{
		public CoffeeWithMilk(Coffee coffee) : base(coffee)
		{
		}

		public override double GetPrice()
		{
			return base.GetPrice() + 0.5;
		}
	}

	class SimpleCoffee : Coffee
	{
		public override double GetPrice()
		{
			return 2.0;
		}
	}

	class VehicleWithThreeWheels : VehicleDecorator
	{
		public VehicleWithThreeWheels(IVehicle vehicle) : base(vehicle)
		{
		}

		public override int GetNumberOfWheels()
		{
			return 3;
		}
		
	}

	[Decorator]
	public abstract class Coffee
	{
		public abstract double GetPrice();
	}

	[Decorator]
	interface IVehicle
	{
		void Drive();
		int GetNumberOfWheels();
	}
}