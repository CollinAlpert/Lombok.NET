using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Lombok.NET.PropertyGenerators
{
	[Generator]
	public class NotifyPropertyChangingGenerator : BasePropertyChangeGenerator
	{
		public const string SetFieldMethodName = "SetFieldAndRaisePropertyChanging";
		
		protected override BaseAttributeSyntaxReceiver SyntaxReceiver { get; } = new NotifyPropertyChangingSyntaxReceiver();
		
		protected override IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment, ExpressionStatementSyntax propertyChangeCall)
		{
			return new[] { propertyChangeCall, newValueAssignment };
		}
		
		protected override MethodDeclarationSyntax CreateSetFieldMethod()
		{
			return MethodDeclaration(
				PredefinedType(
					Token(SyntaxKind.VoidKeyword)
				),
				Identifier(SetFieldMethodName)
			);
		}
	}
}