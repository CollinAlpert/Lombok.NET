using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET.PropertyGenerators
{
	[Generator]
	public class NotifyPropertyChangingGenerator : BasePropertyChangeGenerator
	{
		protected override BaseAttributeSyntaxReceiver SyntaxReceiver { get; } = new NotifyPropertyChangingSyntaxReceiver();
		
		protected override IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment, ExpressionStatementSyntax propertyChangeCall)
		{
			return new[] { propertyChangeCall, newValueAssignment };
		}
	}
}