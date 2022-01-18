using Xunit;

namespace Lombok.NET.Test;

public class NotifyPropertyChangeTest
{
	[Fact]
	public void TestPropertyChangedEvent()
	{
		var vm = new PropertyChangedViewModel();
		var eventWasRaised = false;
		vm.PropertyChanged += (sender, args) => eventWasRaised = true;
		vm.Name = "Test";
		
		Assert.True(eventWasRaised);
		Assert.Equal("Test", vm.Name);
	}

	[Fact]
	public void TestPropertyChangingEvent()
	{
		var vm = new PropertyChangingViewModel();
		var eventWasRaised = false;
		vm.PropertyChanging += (sender, args) => eventWasRaised = true;
		vm.Name = "Test";
		
		Assert.True(eventWasRaised);
		Assert.Equal("Test", vm.Name);
	}
}

[NotifyPropertyChanged]
partial class PropertyChangedViewModel
{
	[Property(PropertyChangeType.PropertyChanged)]
	private string _name;
}

[NotifyPropertyChanging]
partial class PropertyChangingViewModel
{
	[Property(PropertyChangeType.PropertyChanging)]
	private string _name;
}