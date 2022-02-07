using Lombok.NET.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Lombok.NET.Test.Analyzers;

public class AsyncMethodMustBeInPartialClassOrStructTest : LombokAnalyzerVerifier<AsyncMethodMustBeInPartialClassOrStructAnalyzer>
{
	private const string LocalMethodCode = @"
using Lombok.NET;

namespace Test;

public class MyViewModel {
	
	public void MyMethod() {

		[Async]
		void {|#0:Test|}() {
	
		}
	}
}
";
	
	private const string NotInClassCode = @"
using Lombok.NET;

namespace Test;

public interface IRepository {
	
	[Async]
	void {|#0:Test|}();
}
";
	
	private const string ClassNotPartialCode = @"
using Lombok.NET;

namespace Test;

public class {|#0:Test|} {
	
	[Async]
	void Run() {
	}
}
";
	
	private const string ValidCode = @"
using Lombok.NET;

namespace Test;

public partial class MyViewModel {
	
	[Async]
	public void Test() {
	}
}
";
	
	[Theory]
	[InlineData(LocalMethodCode, "LOM004")]
	[InlineData(NotInClassCode, "LOM004")]
	[InlineData(ClassNotPartialCode, "LOM001")]
	public Task WillRaiseDiagnostic(string code, string diagnosticId)
	{
		var expectedDiagnostic = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error);
		expectedDiagnostic = expectedDiagnostic
			.WithLocation(0)
			.WithArguments("Test");

		return VerifyAnalyzerAsync(code, expectedDiagnostic);
	}
	
	[Theory]
	[InlineData(ValidCode)]
	public Task WillNotRaiseDiagnostic(string code)
	{
		return VerifyAnalyzerAsync(code);
	}
}