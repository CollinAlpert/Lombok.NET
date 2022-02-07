using Lombok.NET.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Lombok.NET.Test.Analyzers;

public class PropertyFieldMustBeInPartialClassOrStructTest : LombokAnalyzerVerifier<PropertyFieldMustBeInPartialClassOrStructAnalyzer>
{
	private const string FieldInRecordCode = @"
using Lombok.NET;

namespace Test;

public record MyRecord {

	[Property]
	private {|#0:int _value|};
}
";

	private const string FieldInRecordCode2 = @"
using Lombok.NET;

namespace Test;

public record MyRecord {

	[Property]
	private {|#0:string _stringValue, _anotherStringValue|};
}
";

	private const string ClassNotPartialCode = @"
using Lombok.NET;

namespace Test;

public class {|#0:MyType|} {

	[Property]
	private string _stringValue, _anotherStringValue;
}
";

	private const string StructNotPartialCode = @"
using Lombok.NET;

namespace Test;

public struct {|#0:MyType|} {

	[Property]
	private string _stringValue, _anotherStringValue;
}
";

	private const string ValidCode = @"
using Lombok.NET;

namespace Test;

public partial class MyViewModel {
	
	[Property]
	private int _value;
}
";

	private const string ValidCode2 = @"
using Lombok.NET;

namespace Test;

public partial struct MyStruct {
	
	[Property]
	private int _value;
}
";
	
	[Fact]
	public Task WillRaiseDiagnostic()
	{
		var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.PropertyFieldMustBeInClassOrStruct);
		expectedDiagnostic = expectedDiagnostic
			.WithLocation(0)
			.WithArguments("_value");

		return VerifyAnalyzerAsync(FieldInRecordCode, expectedDiagnostic);
	}
	
	[Fact]
	public Task WillRaiseDiagnostic2()
	{
		var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.PropertyFieldMustBeInClassOrStruct);
		expectedDiagnostic = expectedDiagnostic
			.WithLocation(0)
			.WithArguments("_stringValue, _anotherStringValue");

		return VerifyAnalyzerAsync(FieldInRecordCode2, expectedDiagnostic);
	}
	
	[Theory]
	[InlineData(ClassNotPartialCode)]
	[InlineData(StructNotPartialCode)]
	public Task WillRaiseDiagnostic3(string code)
	{
		var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.TypeMustBePartial);
		expectedDiagnostic = expectedDiagnostic
			.WithLocation(0)
			.WithArguments("MyType");

		return VerifyAnalyzerAsync(code, expectedDiagnostic);
	}
	
	[Theory]
	[InlineData(ValidCode)]
	[InlineData(ValidCode2)]
	public Task WillNotRaiseDiagnostic(string code)
	{
		return VerifyAnalyzerAsync(code);
	}
}