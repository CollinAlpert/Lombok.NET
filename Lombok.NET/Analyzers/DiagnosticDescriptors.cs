using Microsoft.CodeAnalysis;

namespace Lombok.NET.Analyzers
{
	public static class DiagnosticDescriptors
	{
		public static readonly DiagnosticDescriptor TypeMustBePartial = new(
			"LOM001",
			"Type must be partial",
			"The type '{0}' must be partial in order to generate code for it",
			"Usage",
			DiagnosticSeverity.Error,
			true
		);
		
		public static readonly DiagnosticDescriptor TypeMustBeNonNested = new(
			"LOM002",
			"Type must be non-nested",
			"The type '{0}' must be non-nested in order to generate code for it",
			"Usage",
			DiagnosticSeverity.Error,
			true
		);
		
		public static readonly DiagnosticDescriptor TypeMustHaveNamespace = new(
			"LOM003",
			"Type must have namespace",
			"The type '{0}' must be in a namespace in order to generate code for it",
			"Usage",
			DiagnosticSeverity.Error,
			true
		);
		
		public static readonly DiagnosticDescriptor AsyncMethodMustBeInClassOrStruct = new(
			"LOM004",
			"Method must be inside class or struct",
			"The method '{0}' must be inside a class or a struct and cannot be a local function",
			"Usage",
			DiagnosticSeverity.Error,
			true
		);
		
		public static readonly DiagnosticDescriptor PropertyFieldMustBeInClassOrStruct = new(
			"LOM005",
			"Field must be inside class or struct",
			"The field '{0}' must be inside a class or a struct",
			"Usage",
			DiagnosticSeverity.Error,
			true
		);
	}
}