using Microsoft.CodeAnalysis;

namespace Lombok.NET.Extensions;

/// <summary>
/// A cache of type symbols.
/// </summary>
internal static class SymbolCache
{
	/// <summary>
	/// The symbol for the [AllArgsConstructor] attribute.
	/// </summary>
	public static INamedTypeSymbol? AllArgsConstructorAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [NoArgsConstructor] attribute.
	/// </summary>
	public static INamedTypeSymbol? NoArgsConstructorAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [RequiredArgsConstructor] attribute.
	/// </summary>
	public static INamedTypeSymbol? RequiredArgsConstructorAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [Async] attribute.
	/// </summary>
	public static INamedTypeSymbol? AsyncAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [AsyncOverloads] attribute.
	/// </summary>
	public static INamedTypeSymbol? AsyncOverloadsAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [ToString] attribute.
	/// </summary>
	public static INamedTypeSymbol? ToStringAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [With] attribute.
	/// </summary>
	public static INamedTypeSymbol? WithAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [NotifyPropertyChanged] attribute.
	/// </summary>
	public static INamedTypeSymbol? NotifyPropertyChangedAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [NotifyPropertyChanging] attribute.
	/// </summary>
	public static INamedTypeSymbol? NotifyPropertyChangingAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [Property] attribute.
	/// </summary>
	public static INamedTypeSymbol? PropertyAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [Singleton] attribute.
	/// </summary>
	public static INamedTypeSymbol? SingletonAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [Decorator] attribute.
	/// </summary>
	public static INamedTypeSymbol? DecoratorAttributeSymbol;
		
	/// <summary>
	/// The symbol for the [Lazy] attribute.
	/// </summary>
	public static INamedTypeSymbol? LazyAttributeSymbol;
}