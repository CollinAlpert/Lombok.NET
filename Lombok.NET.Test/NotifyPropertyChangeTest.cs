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
}

[NotifyPropertyChanged]
partial class PropertyChangedViewModel
{
	[Property(PropertyChangeType.PropertyChanged)]
	private string _name;

	[Property(PropertyChangeType.PropertyChanged)]
	private HttpStatusCode _statusCode;
}

[NotifyPropertyChanging]
partial class PropertyChangingViewModel
{
	[Property(PropertyChangeType.PropertyChanging)]
	private string _name;
	
	[Property(PropertyChangeType.PropertyChanging)]
	private HttpStatusCode _statusCode;
}