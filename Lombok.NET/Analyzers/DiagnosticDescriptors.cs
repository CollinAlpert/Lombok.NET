using Microsoft.CodeAnalysis;

namespace Lombok.NET.Analyzers;

/// <summary>
/// Contains definitions of diagnostics which can be raised by Lombok.NET.
/// </summary>
public static class DiagnosticDescriptors
{
	/// <summary>
	/// Raised when a type is not partial although it should be.
	/// </summary>
	public static readonly DiagnosticDescriptor TypeMustBePartial = new(
		"LOM001",
		"Type must be partial",
		"The type '{0}' must be partial in order to generate code for it",
		"Usage",
		DiagnosticSeverity.Error,
		true
	);
		
	/// <summary>
	/// Raised when a type is within another type although it should not be.
	/// </summary>
	public static readonly DiagnosticDescriptor TypeMustBeNonNested = new(
		"LOM002",
		"Type must be non-nested",
		"The type '{0}' must be non-nested in order to generate code for it",
		"Usage",
		DiagnosticSeverity.Error,
		true
	);
		
	/// <summary>
	/// Raised when a type is not within a namespace although it should be.
	/// </summary>
	public static readonly DiagnosticDescriptor TypeMustHaveNamespace = new(
		"LOM003",
		"Type must have namespace",
		"The type '{0}' must be in a namespace in order to generate code for it",
		"Usage",
		DiagnosticSeverity.Error,
		true
	);
		
	/// <summary>
	/// Raised when a method is not within a class or a struct although it should be.
	/// </summary>
	public static readonly DiagnosticDescriptor AsyncMethodMustBeInClassOrStruct = new(
		"LOM004",
		"Method must be inside class or struct",
		"The method '{0}' must be inside a class or a struct and cannot be a local function",
		"Usage",
		DiagnosticSeverity.Error,
		true
	);
		
	/// <summary>
	/// Raised when a field is not within a class or a struct although it should be.
	/// </summary>
	public static readonly DiagnosticDescriptor PropertyFieldMustBeInClassOrStruct = new(
		"LOM005",
		"Field must be inside class or struct",
		"The field '{0}' must be inside a class or a struct",
		"Usage",
		DiagnosticSeverity.Error,
		true
	);
}