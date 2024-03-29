using System.Net;
using Xunit;

namespace Lombok.NET.Test;

public class NotifyPropertyChangeTest
{
	[Fact]
	public void TestNamePropertyChangedEvent()
	{
		var vm = new PropertyChangedViewModel();
		var eventWasRaised = false;
		vm.PropertyChanged += (sender, args) => eventWasRaised = true;
		vm.Name = "Test";

		Assert.True(eventWasRaised);
		Assert.Equal("Test", vm.Name);
	}
	
	[Fact]
	public void TestStatusCodePropertyChangedEvent()
	{
		var vm = new PropertyChangedViewModel();
		var eventWasRaised = false;
		vm.PropertyChanged += (sender, args) => eventWasRaised = true;
		vm.StatusCode = HttpStatusCode.Accepted;

		Assert.True(eventWasRaised);
		Assert.Equal(HttpStatusCode.Accepted, vm.StatusCode);
	}

	[Fact]
	public void TestNamePropertyChangingEvent()
	{
		var vm = new PropertyChangingViewModel();
		var eventWasRaised = false;
		vm.PropertyChanging += (sender, args) => eventWasRaised = true;
		vm.Name = "Test";

		Assert.True(eventWasRaised);
		Assert.Equal("Test", vm.Name);
	}

	[Fact]
	public void TestStatusCodePropertyChangingEvent()
	{
		var vm = new PropertyChangingViewModel();
		var eventWasRaised = false;
		vm.PropertyChanging += (sender, args) => eventWasRaised = true;
		vm.StatusCode = HttpStatusCode.Accepted;

		Assert.True(eventWasRaised);
		Assert.Equal(HttpStatusCode.Accepted, vm.StatusCode);
	}

	[Fact]
	public void TestReactivePropertyChange()
	{
		var vm = new ReactivePropertyChangeViewModel();
		var eventWasRaised = false;
		vm.PropertyChanging += (sender, args) => eventWasRaised = true;
		vm.Name = "Bob";

		Assert.True(eventWasRaised);
		Assert.Equal("Bob", vm.Name);
	}
}

[NotifyPropertyChanged]
partial class PropertyChangedViewModel
{
	[Property(PropertyChangeType = PropertyChangeType.PropertyChanged)]
	private string _name;

	[Property(PropertyChangeType = PropertyChangeType.PropertyChanged)]
	private HttpStatusCode _statusCode;
}

[NotifyPropertyChanging]
partial class PropertyChangingViewModel
{
	[Property(PropertyChangeType = PropertyChangeType.PropertyChanging)]
	private string _name;
	
	[Property(PropertyChangeType = PropertyChangeType.PropertyChanging)]
	private HttpStatusCode _statusCode;
}

partial class ReactivePropertyChangeViewModel : ReactiveUI.ReactiveObject
{
	[Property(PropertyChangeType = PropertyChangeType.ReactivePropertyChange)]
	private string _name;
}