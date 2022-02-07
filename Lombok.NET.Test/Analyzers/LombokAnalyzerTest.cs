using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Lombok.NET.Test.Analyzers;

public class LombokAnalyzerTest<TAnalyzer> : AnalyzerTest<XUnitVerifier> 
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	public LombokAnalyzerTest()
	{
		TestState.AdditionalReferences.Add(Assembly.Load("Lombok.NET"));
	}
	
	protected override CompilationOptions CreateCompilationOptions()
	{
		return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
	}

	protected override ParseOptions CreateParseOptions()
	{
		return new CSharpParseOptions();
	}

	protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { new TAnalyzer() };

	protected override string DefaultFileExt { get; } = "cs";
	public override string Language { get; } = LanguageNames.CSharp;
}