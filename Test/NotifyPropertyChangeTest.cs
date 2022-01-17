using System;
using Lombok.NET;

namespace Test;

public class NotifyPropertyChangeTest
{
	public NotifyPropertyChangeTest()
	{
		var vm = new MyPropertyChangedViewModel();
		vm.PropertyChanged += (o, args) => Console.WriteLine("Property was changed!");

		vm.Name = "Collin";
	}
}

[NotifyPropertyChanged]
partial class MyPropertyChangedViewModel
{
	[Property(PropertyChangeType.PropertyChanged)]
	private string _name;
}