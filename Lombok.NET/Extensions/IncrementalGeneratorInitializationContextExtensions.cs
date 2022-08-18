using Microsoft.CodeAnalysis;

namespace Lombok.NET.Extensions;

/// <summary>
/// Extensions for <see cref="IncrementalGeneratorInitializationContext"/>.
/// </summary>
internal static class IncrementalGeneratorInitializationContextExtensions
{
	/// <summary>
	/// Checks if the result is erroneous and if a diagnostic needs to be raised. If not, it adds the source to the compilation.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="provider"></param>
	public static void AddSources(this IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<GeneratorResult> provider)
	{
		context.RegisterSourceOutput(provider, AddSources);
	}

	private static void AddSources(SourceProductionContext context, GeneratorResult result)
	{
		if (result.IsValid)
		{
			context.AddSource($"{result.TypeName}.g.cs", result.Source);
		}
		else if(result.Diagnostic is not null)
		{
			context.ReportDiagnostic(result.Diagnostic);
		}
	}
}