using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Lombok.NET.Test.Analyzers;

public class LombokAnalyzerVerifier<TAnalyzer> : AnalyzerVerifier<TAnalyzer, LombokAnalyzerTest<TAnalyzer>, XUnitVerifier> 
	where TAnalyzer : DiagnosticAnalyzer, new()
{
}