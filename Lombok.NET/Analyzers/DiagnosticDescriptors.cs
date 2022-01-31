using Microsoft.CodeAnalysis;

namespace Lombok.NET.Analyzers
{
	public static class DiagnosticDescriptors
	{
		public static readonly DiagnosticDescriptor TypeMustBePartial = new DiagnosticDescriptor(
			"LOM001",
			"Type must be partial",
			"The type '{0}' must be partial in order to generate code for it",
			"Usage",
			DiagnosticSeverity.Error,
			true
		);
	}
}