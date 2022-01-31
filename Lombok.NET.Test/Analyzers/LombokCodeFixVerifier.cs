using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Lombok.NET.Test.Analyzers;

public class LombokCodeFixVerifier<TAnalyzer, TCodeFix> : CodeFixVerifier<TAnalyzer, TCodeFix, LombokCodeFixTest<TAnalyzer, TCodeFix>, XUnitVerifier>
	where TAnalyzer : DiagnosticAnalyzer, new() 
	where TCodeFix : CodeFixProvider, new()
{
}