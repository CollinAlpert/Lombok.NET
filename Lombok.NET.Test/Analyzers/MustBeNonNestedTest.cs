using Lombok.NET.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Lombok.NET.Test.Analyzers;

public class MustBeNonNestedTest : LombokAnalyzerVerifier<MustBeNonNestedAnalyzer>
{

	[Theory]
	[InlineData("With")]
	[InlineData("AllArgsConstructor")]
	[InlineData("RequiredArgsConstructor")]
	[InlineData("NoArgsConstructor")]
	[InlineData("ToString")]
	[InlineData("Singleton")]
	[InlineData("NotifyPropertyChangedAttribute")]
	[InlineData("NotifyPropertyChangingAttribute")]
	public Task WillRaise(string attribute)
	{
		const string source = @"
using Lombok.NET;

namespace Test;

public class MyClass {{

	[{0}]
	public class {{|#0:Nested|}} {{
	}}
}}
";
		var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.TypeMustBeNonNested);
		expectedDiagnostic = expectedDiagnostic
			.WithLocation(0)
			.WithArguments("Nested");

		return VerifyAnalyzerAsync(string.Format(source, attribute), expectedDiagnostic);
	}
}