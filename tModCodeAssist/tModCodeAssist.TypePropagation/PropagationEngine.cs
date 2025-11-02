using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace tModCodeAssist.TypePropagation;

// TODO: Flow/more expression analysis.
// Method(expr) -> expr : ID? for example

/// <summary>
///		Responsible for determining type propagation.
/// </summary>
public static class PropagationEngine
{
	private record struct Context(SemanticModel Model, SymbolTracker Tracker)
	{
		public int Changes { get; set; }

		public void Update(ISymbol symbol, IdKind kind)
		{
			if (kind == IdKind.Unknown || !kind.IsSingle())
				return;

			if (IsGenericOrArrayIndex(symbol))
				return;

			if (Tracker.TryUpdate(symbol, kind))
				Changes++;
		}
	}

	// This is a bit of a code-smell: contains types which we know will not
	// contain members that should be mapped to ID types.  Should be kept small,
	// only the bare minimum required to not produce erroneous maps or
	// ambiguities.
	// TODO: Change logic to only care about the first input project?  Should we
	//       support mods in the future, though?
	private static readonly HashSet<string> blacklisted_types = [
		"System.Array",
		"System.Linq.Enumerable",
		"System.IO.BinaryWriter",
		"System.Math",
		"System.IO.Stream",
		"System.IO.FileStream",
	];

	/// <summary>
	///		Propagates type inference on the compilation, mutating the ctx.Tracker.
	/// </summary>
	/// <returns>Whether any changes were made.</returns>
	public static bool PropagateOnce(
		Compilation compilation,
		SymbolTracker tracker,
		out int updates
	)
	{
		updates = 0;

		foreach (SyntaxTree? tree in compilation.SyntaxTrees) {
			SemanticModel model = compilation.GetSemanticModel(tree);

			foreach (SyntaxNode? node in tree.GetRoot().DescendantNodes()) {
				var context = new Context(model, tracker);

				switch (node) {
					// int a = b;
					case VariableDeclaratorSyntax variableDeclaratorSyntax:
						HandleVariableDeclaration(ref context, variableDeclaratorSyntax);
						break;

					// a = b;
					case AssignmentExpressionSyntax assignmentExpressionSyntax:
						HandleAssignmentExpression(ref context, assignmentExpressionSyntax);
						break;

					// Method(arg);
					case InvocationExpressionSyntax invocationExpressionSyntax:
						HandleInvocationExpression(ref context, invocationExpressionSyntax);
						break;

					// return expr;
					case ReturnStatementSyntax returnStatementSyntax:
						HandleReturnStatement(ref context, returnStatementSyntax);
						break;
				}

				updates += context.Changes;
			}
		}

		return updates > 0;
	}

	private static void HandleVariableDeclaration(
		ref Context ctx,
		VariableDeclaratorSyntax syntax
	)
	{
		if (syntax.Initializer is null)
			return;

		ISymbol? left = ctx.Model.GetDeclaredSymbol(syntax);
		ISymbol? right = ctx.Model.GetSymbolInfo(syntax.Initializer.Value).Symbol;
		if (left is null || right is null)
			return;

		if (!ShouldPropagate(right, left, ctx.Tracker))
			return;

		ctx.Update(left, ctx.Tracker.GetKind(right));
	}

	private static void HandleAssignmentExpression(
		ref Context ctx,
		AssignmentExpressionSyntax syntax
	)
	{
		ISymbol? left = ctx.Model.GetSymbolInfo(syntax.Left).Symbol;
		ISymbol? right = ctx.Model.GetSymbolInfo(syntax.Right).Symbol;
		if (left is null || right is null)
			return;

		if (!ShouldPropagate(right, left, ctx.Tracker))
			return;

		ctx.Update(left, ctx.Tracker.GetKind(right));
	}

	private static void HandleInvocationExpression(
		ref Context ctx,
		InvocationExpressionSyntax syntax
	)
	{
		if (ctx.Model.GetSymbolInfo(syntax).Symbol is not IMethodSymbol method)
			return;

		SeparatedSyntaxList<ArgumentSyntax> args = syntax.ArgumentList.Arguments;
		for (int i = 0; i < args.Count && i < method.Parameters.Length; i++) {
			IParameterSymbol param = method.Parameters[i];
			ExpressionSyntax argExpr = args[i].Expression;

			ISymbol? argSymbol = ctx.Model.GetSymbolInfo(argExpr).Symbol;
			if (argSymbol is null)
				continue;

			// param -> arg
			if (ShouldPropagate(param, argSymbol, ctx.Tracker))
				ctx.Update(argSymbol, ctx.Tracker.GetKind(param));

			// arg -> param
			if (ShouldPropagate(argSymbol, param, ctx.Tracker))
				ctx.Update(param, ctx.Tracker.GetKind(argSymbol));
		}

		// a = Method(arg);
		if (syntax.Parent is not AssignmentExpressionSyntax retAssign)
			return;

		ISymbol? left = ctx.Model.GetSymbolInfo(retAssign.Left).Symbol;
		if (left is null || !ShouldPropagate(method, left, ctx.Tracker))
			return;

		ctx.Update(left, ctx.Tracker.GetKind(method));
	}

	private static void HandleReturnStatement(
		ref Context ctx,
		ReturnStatementSyntax syntax
	)
	{
		if (syntax.Expression is not { } expr)
			return;

		if (ctx.Model.GetSymbolInfo(expr).Symbol is not { } exprSymbol)
			return;

		if (ctx.Model.GetEnclosingSymbol(expr.SpanStart) is not IMethodSymbol method)
			return;

		if (!ShouldPropagate(exprSymbol, method, ctx.Tracker))
			return;

		ctx.Update(method, ctx.Tracker.GetKind(exprSymbol));
	}

	private static bool ShouldPropagate(ISymbol? from, ISymbol? to, SymbolTracker tracker)
	{
		if (from is null || to is null)
			return false;

		if (IsNonValueSymbol(from) || IsNonValueSymbol(to))
			return false;

		ITypeSymbol? fromType = GetTypeOf(from);
		ITypeSymbol? toType = GetTypeOf(to);
		if (fromType == null || toType == null)
			return false;

		if (!IsNumericOrEnum(fromType) || !IsNumericOrEnum(toType))
			return false;

		if (IsGenericOrArrayIndex(from) || IsGenericOrArrayIndex(to))
			return false;

		if ((from.ContainingType != null && blacklisted_types.Contains(from.ContainingType.ToDisplayString())) ||
		    (to.ContainingType != null && blacklisted_types.Contains(to.ContainingType.ToDisplayString())))
			return false;

		IdKind fromKind = tracker.GetKind(from);
		if (!fromKind.IsSingle())
			return false;

		return true;
	}

	private static bool IsGenericOrArrayIndex(ISymbol symbol)
	{
		// if it's in a generic type at all TODO: be more permissive?
		if (symbol.ContainingType is { IsGenericType: true })
			return true;

		// generic methods
		if (symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { IsGenericMethod: true } })
			return true;

		// type parameters, should never really pop up? but eh
		if (symbol is ITypeParameterSymbol)
			return true;

		// compiler-generated variables and variables that are type params
		if (symbol is ILocalSymbol local && (local.Name.StartsWith("<>") || local.Type.TypeKind == TypeKind.TypeParameter))
			return true;

		// array/pointer fields TODO: look into caring about these
		if (symbol is IFieldSymbol field && (field.Type is IArrayTypeSymbol || field.Type is IPointerTypeSymbol))
			return true;

		return false;
	}

	private static bool IsNonValueSymbol(ISymbol symbol) =>
		symbol is ITypeSymbol or INamespaceSymbol or IMethodSymbol;

	private static ITypeSymbol? GetTypeOf(ISymbol? symbol)
	{
		return symbol switch {
			IFieldSymbol field => field.Type,
			IPropertySymbol property => property.Type,
			IParameterSymbol parameter => parameter.Type,
			ILocalSymbol local => local.Type,
			IMethodSymbol method => method.ReturnType,
			_ => null,
		};
	}

	private static bool IsNumericOrEnum(ITypeSymbol type) =>
		type.TypeKind == TypeKind.Enum || IsNumericType(type.SpecialType);

	internal static bool IsNumericType(SpecialType type)
	{
		return type is SpecialType.System_Byte
		            or SpecialType.System_SByte
		            or SpecialType.System_Int16
		            or SpecialType.System_UInt16
		            or SpecialType.System_Int32
		            or SpecialType.System_UInt32
		            or SpecialType.System_Int64
		            or SpecialType.System_UInt64;
	}
}