using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lombok.NET.PropertyGenerators
{
	[Generator]
	public class NotifyPropertyChangedGenerator : BasePropertyChangeGenerator
	{
		protected override BaseAttributeSyntaxReceiver SyntaxReceiver { get; } = new NotifyPropertyChangedSyntaxReceiver();
		
		protected override IEnumerable<StatementSyntax> CreateAssignmentWithPropertyChangeMethod(ExpressionStatementSyntax newValueAssignment, ExpressionStatementSyntax propertyChangeCall)
		{
			return new[] { newValueAssignment, propertyChangeCall };
		}
	}
}