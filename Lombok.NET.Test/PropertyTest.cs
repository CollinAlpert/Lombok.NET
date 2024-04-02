using Lombok.NET.PropertyGenerators;

namespace Lombok.NET.Test;

public class PropertyTest
{
	[Fact]
	public Task ClassTest()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      partial class PropertyPerson
		                      {
		                      	  [Property]
		                      	  private string _name;
		                      
		                      	  [Property]
		                      	  private readonly int _age;
		                      
		                      	  [Property]
		                      	  private readonly HttpStatusCode _statusCode;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}

	[Fact]
	public Task TestWithoutModifier()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;
		                      
		                      namespace Test;
		                      
		                      partial class PropertyPerson
		                      {
		                      	  [Property]
		                      	  string _name;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}
	
	[Fact]
	public Task StructTest()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;

		                      namespace Test;

		                      partial struct PropertyPersonStruct
		                      {
		                      	  [Property]
		                      	  private string _name;
		                      
		                      	  [Property]
		                      	  private readonly int _age;
		                      	
		                      	  [Property]
		                      	  private readonly HttpStatusCode _statusCode;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}

	[Fact]
	public Task TestWithComments()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;

		                      namespace Test;

		                      partial class PropertyPersonWithComments
		                      {
		                      	  [Property]
		                      	  /*
		                      	   * The person's name.
		                      	   */
		                      	  string _name;
		                      	
		                      	  [Property]
		                      	  /// <summary>
		                      	  /// The person's height.
		                      	  /// </summary>
		                      	  int _height;
		                      
		                      	  [Property]
		                      	  /// <summary>
		                      	  /// The person's age.
		                      	  /// </summary>
		                      	  private readonly int _age;
		                      
		                      	  // The person's status code.
		                      	  [Property]
		                      	  private readonly HttpStatusCode _statusCode;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}

	[Fact]
	public Task TestWithValidationAttributes()
	{
		const string source = """
		                      using System.ComponentModel.DataAnnotations;
		                      using Lombok.NET;

		                      namespace Test;

		                      partial class PropertyPersonWithValidationAttributes
		                      {
		                      	  [Property]
		                      	  [System.ComponentModel.DataAnnotations.MaxLength(20)]
		                      	  private string _name;
		                      	
		                      	  [Property]
		                      	  [EmailAddress]
		                      	  private string _email;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}

	[Fact]
	public Task TestWithReactivePropertyChanged()
	{
		const string source = """
		                      using Lombok.NET;

		                      namespace Test;

		                      partial class ReactivePropertyChangeViewModel : ReactiveUI.ReactiveObject
		                      {
		                      	  [Property(PropertyChangeType = PropertyChangeType.ReactivePropertyChange)]
		                      	  private string _name;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}

	[Fact]
	public Task TestWithPropertyChanged()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;

		                      namespace Test;

		                      partial class PropertyChangedViewModel
		                      {
		                      	  [Property(PropertyChangeType = PropertyChangeType.PropertyChanged)]
		                      	  private string _name;
		                      
		                          [Property(PropertyChangeType = PropertyChangeType.PropertyChanged)]
		                          private HttpStatusCode _statusCode;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}

	[Fact]
	public Task TestWithPropertyChanging()
	{
		const string source = """
		                      using System.Net;
		                      using Lombok.NET;

		                      namespace Test;

		                      partial class PropertyChangingViewModel
		                      {
		                      	  [Property(PropertyChangeType = PropertyChangeType.PropertyChanging)]
		                      	  private string _name;
		                      
		                          [Property(PropertyChangeType = PropertyChangeType.PropertyChanging)]
		                          private HttpStatusCode _statusCode;
		                      }
		                      """;

		return TestHelper.Verify<PropertyGenerator>(source);
	}
}