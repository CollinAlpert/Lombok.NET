using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Lombok.NET.Test;

internal static class TestHelper
{
	public static async Task Verify<TGenerator>(string source, bool expectEmptyResult = false)
		where TGenerator : IIncrementalGenerator, new()
	{
		string mscorLibLocation = typeof(object).Assembly.Location;
		string netLocation = Path.GetDirectoryName(mscorLibLocation)!;
		IEnumerable<PortableExecutableReference> references = 
		[
			MetadataReference.CreateFromFile(mscorLibLocation),
			MetadataReference.CreateFromFile(Path.Combine(netLocation, "netstandard.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netLocation, "System.Console.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netLocation, "System.Net.Primitives.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netLocation, "System.Runtime.dll")),
			MetadataReference.CreateFromFile(typeof(AllArgsConstructorAttribute).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(ReactiveUI.ReactiveObject).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.EmailAddressAttribute).Assembly.Location)
		];
		
		CSharpCompilation compilation = CSharpCompilation.Create("Tests", [CSharpSyntaxTree.ParseText(source)], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		EmitResult compilationResult = compilation.Emit(Stream.Null);
		Diagnostic[] errors = compilationResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
		if (errors.Length > 0)
		{
			Assert.Fail(string.Join('\n', errors.Select(e => e.GetMessage())));
		}

		GeneratorDriver driver = CSharpGeneratorDriver.Create(new TGenerator()).RunGenerators(compilation);

		var verifyResult = await Verifier.Verify(driver).UseDirectory("Snapshots");
		if (!expectEmptyResult && !verifyResult.Files.Any())
		{
			Assert.Fail("Generator emitted no files.");
		}
		
		if (expectEmptyResult && verifyResult.Files.Any())
		{
			Assert.Fail("Generator emitted files.");
		}
	}
}