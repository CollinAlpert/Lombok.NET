using Lombok.NET.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Lombok.NET.Test.Analyzers;

public class MustHaveNamespaceTest : LombokAnalyzerVerifier<MustHaveNamespaceAnalyzer>
{
	public static readonly IEnumerable<object[]> Attributes = new[]
	{
		new[] { "Decorator" },
		new[] { "With" },
		new[] { "AllArgsConstructor" },
		new[] { "RequiredArgsConstructor" },
		new[] { "NoArgsConstructor" },
		new[] { "ToString" },
		new[] { "Singleton" },
		new[] { "NotifyPropertyChangedAttribute" },
		new[] { "NotifyPropertyChangingAttribute" }
	};

	[Theory]
	[MemberData(nameof(Attributes))]
	public Task WillRaiseDiagnostic(string attribute)
	{
		const string source = @"
using Lombok.NET;

[{0}]
public class {{|#0:MyClass|}} {{
}}
";
		var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.TypeMustHaveNamespace);
		expectedDiagnostic = expectedDiagnostic
			.WithLocation(0)
			.WithArguments("MyClass");

		return VerifyAnalyzerAsync(string.Format(source, attribute), expectedDiagnostic);
	}

	[Theory]
	[MemberData(nameof(Attributes))]
	public Task WillNotRaiseDiagnostic(string attribute)
	{
		const string source = @"
using Lombok.NET;

namespace Test;

[{0}]
public class MyClass {{
}}
";

		return VerifyAnalyzerAsync(string.Format(source, attribute));
	}
}