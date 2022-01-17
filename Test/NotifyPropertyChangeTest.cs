using Lombok.NET;

namespace Test;

public class NotifyPropertyChangeTest
{
	
}

[NotifyPropertyChanging]
public partial class MyPropertyChangedViewModel
{
	[Property(PropertyChangeType.PropertyChanging)]
	private string _name;
}