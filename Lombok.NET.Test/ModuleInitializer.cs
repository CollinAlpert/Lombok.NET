using System.Runtime.CompilerServices;

namespace Lombok.NET.Test;

internal static class ModuleInitializer
{
	[ModuleInitializer]
	public static void Init()
	{
		VerifySourceGenerators.Initialize();
	}
}