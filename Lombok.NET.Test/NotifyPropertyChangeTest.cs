using Lombok.NET.PropertyGenerators;

namespace Lombok.NET.Test;

public class NotifyPropertyChangeTest
{
	[Fact]
	public Task TestNotifyPropertyChanged()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [NotifyPropertyChanged]
		                      partial class PropertyChangedViewModel;
		                      """;

		return TestHelper.Verify<NotifyPropertyChangedGenerator>(source);
	}
	
	[Fact]
	public Task TestNotifyPropertyChanging()
	{
		const string source = """
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      [NotifyPropertyChanging]
		                      partial class PropertyChangingViewModel;
		                      """;

		return TestHelper.Verify<NotifyPropertyChangingGenerator>(source);
	}
}