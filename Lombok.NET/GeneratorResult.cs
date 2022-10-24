using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Lombok.NET;

/// <summary>
/// The result class for incremental generators. Either contains source code or a diagnostic which should be raised.
/// </summary>
internal sealed class GeneratorResult
{
	/// <summary>
	/// The name of the generated type.
	/// </summary>
	public string? TypeName { get; }

	/// <summary>
	/// The SourceText of the generated code.
	/// </summary>
	public SourceText? Source { get; }

	/// <summary>
	/// The diagnostic to be raised if something went wrong.
	/// </summary>
	public Diagnostic? Diagnostic { get; }

	/// <summary>
	/// Determines if the result is valid and can be added to the compilation or if the diagnostic needs to be raised.
	/// </summary>
	[MemberNotNullWhen(true, nameof(TypeName), nameof(Source))]
	public bool IsValid => TypeName is not null && Source is not null && Diagnostic is null;

	/// <summary>
	/// An empty result. Something went wrong, however no diagnostic should be reported
	/// </summary>
	public static GeneratorResult Empty { get; } = new();

	/// <summary>
	/// Constructor to be used in case of success.
	/// </summary>
	/// <param name="typeName">The name of the generated type.</param>
	/// <param name="source">The source of the generated code.</param>
	public GeneratorResult(string typeName, SourceText source)
	{
		TypeName = typeName;
		Source = source;
	}

	/// <summary>
	/// Constructor to be used in case of failure.
	/// </summary>
	/// <param name="diagnostic">The diagnostic to be raised.</param>
	public GeneratorResult(Diagnostic diagnostic)
	{
		Diagnostic = diagnostic;
	}

	private GeneratorResult()
	{
	}
}