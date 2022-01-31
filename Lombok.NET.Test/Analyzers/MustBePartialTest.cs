using Lombok.NET.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Lombok.NET.Test.Analyzers;

public class MustBePartialTest : LombokCodeFixVerifier<MustBePartialAnalyzer, MustBePartialCodeFix>
{
	private const string ClassSource = @"
using Lombok.NET;

namespace Test;

[{0}]
public class {{|#0:Person|}} {{
}}
";

	private const string FixedClassSource = @"
using Lombok.NET;

namespace Test;

[{0}]
public partial class Person {{
}}
";

	private const string StructSource = @"
using Lombok.NET;

namespace Test;

[{0}]
public struct {{|#0:Person|}} {{
}}
";

	private const string FixedStructSource = @"
using Lombok.NET;

namespace Test;

[{0}]
public partial struct Person {{
}}
";

	private const string InterfaceSource = @"
using Lombok.NET;

namespace Test;

[{0}]
public interface {{|#0:Person|}} {{
}}
";

	private const string FixedInterfaceSource = @"
using Lombok.NET;

namespace Test;

[{0}]
public partial interface Person {{
}}
";

	public static readonly IEnumerable<object[]> AttributesForClasses = new[]
	{
		CreateTestCase("With", ClassSource, FixedClassSource),
		CreateTestCase("RequiredArgsConstructor", ClassSource, FixedClassSource),
		CreateTestCase("NoArgsConstructor", ClassSource, FixedClassSource),
		CreateTestCase("NotifyPropertyChangedAttribute", ClassSource, FixedClassSource),
		CreateTestCase("NotifyPropertyChangingAttribute", ClassSource, FixedClassSource),
		CreateTestCase("Singleton", ClassSource, FixedClassSource),
		CreateTestCase("AllArgsConstructor", ClassSource, FixedClassSource),
		CreateTestCase("ToString", ClassSource, FixedClassSource)
	};

	public static readonly IEnumerable<object[]> AttributesForStructs = new[]
	{
		CreateTestCase("AllArgsConstructor", StructSource, FixedStructSource),
		CreateTestCase("ToString", StructSource, FixedStructSource)
	};

	public static readonly IEnumerable<object[]> AttributesForInterfaces = new[]
	{
		CreateTestCase("AsyncOverloads", InterfaceSource, FixedInterfaceSource)
	};

	private static object[] CreateTestCase(string attributeName, string source, string fixedSource)
	{
		return new object[] { string.Format(source, attributeName), string.Format(fixedSource, attributeName) };
	}

	[Theory]
	[MemberData(nameof(AttributesForClasses))]
	[MemberData(nameof(AttributesForStructs))]
	[MemberData(nameof(AttributesForInterfaces))]
	public Task WillRaiseDiagnostic(string source, string fixedSource)
	{
		var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.TypeMustBePartial);
		expectedDiagnostic = expectedDiagnostic
			.WithLocation(0)
			.WithArguments("Person");

		return VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
	}

	[Theory]
	[InlineData("With")]
	[InlineData("AllArgsConstructor")]
	[InlineData("RequiredArgsConstructor")]
	[InlineData("NoArgsConstructor")]
	[InlineData("ToString")]
	[InlineData("Singleton")]
	[InlineData("NotifyPropertyChangedAttribute")]
	[InlineData("NotifyPropertyChangingAttribute")]
	public Task WillNotRaiseDiagnostic(string attribute)
	{
		const string source = @"
using Lombok.NET;

namespace Test;

[{0}]
public partial class Person {{
}}
";

		return VerifyAnalyzerAsync(string.Format(source, attribute));
	}

	[Fact]
	public Task WillNotRaiseDiagnostic2()
	{
		const string source = @"
using Lombok.NET;

namespace Test;

[ToString]
public enum Person {
}
";

		return VerifyAnalyzerAsync(source);
	}

	[Fact]
	public Task WillNotRaiseDiagnostic3()
	{
		const string source = @"
using Lombok.NET;

namespace Test;

[Decorator]
public abstract class Person {
}
";

		return VerifyAnalyzerAsync(source);
	}
}