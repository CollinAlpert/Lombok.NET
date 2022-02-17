using Microsoft.CodeAnalysis;

namespace Lombok.NET.Extensions
{
	public static class SymbolCache
	{
		public static INamedTypeSymbol? AllArgsConstructorAttributeSymbol;
		public static INamedTypeSymbol? NoArgsConstructorAttributeSymbol;
		public static INamedTypeSymbol? RequiredArgsConstructorAttributeSymbol;
		public static INamedTypeSymbol? AsyncAttributeSymbol;
		public static INamedTypeSymbol? AsyncOverloadsAttributeSymbol;
		public static INamedTypeSymbol? ToStringAttributeSymbol;
		public static INamedTypeSymbol? WithAttributeSymbol;
		public static INamedTypeSymbol? NotifyPropertyChangedAttributeSymbol;
		public static INamedTypeSymbol? NotifyPropertyChangingAttributeSymbol;
		public static INamedTypeSymbol? PropertyAttributeSymbol;
		public static INamedTypeSymbol? SingletonAttributeSymbol;
		public static INamedTypeSymbol? DecoratorAttributeSymbol;
	}
}