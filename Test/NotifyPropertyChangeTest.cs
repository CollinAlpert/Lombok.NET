using Lombok.NET;

namespace Test;

public class NotifyPropertyChangeTest
{
	[NotifyPropertyChanged]
	partial class MyPropertyChangedViewModel
	{
		
	}
	
	
	[NotifyPropertyChanging]
	partial class MyPropertyChangingViewModel
	{
		
	}
}