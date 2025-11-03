using System.Collections.Generic;
using System.Diagnostics;
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
	private record struct Context(
		SemanticModel Model,
		SymbolTracker Tracker,
		SymbolExceptionRegistry Exceptions,
		PropagationTraceGraph Trace
	)
	{
		public int Changes { get; set; }

		public void Update(ISymbol from, ISymbol to, string reason)
		{
			IdKind kind = Tracker.GetKind(from);
			if (kind == IdKind.Unknown || !kind.IsSingle())
				return;

			if (!Tracker.TryUpdate(to, kind))
				return;

			Changes++;
			Trace.AddTrace(from, to, kind, reason);
		}
	}

	private static readonly SymbolExceptionRegistry.AssemblyIdentity tmodloader = new("tModLoader");
	private static readonly SymbolExceptionRegistry.TypeIdentity terraria_netmessage = new(tmodloader, "Terraria.NetMessage");
	private static readonly SymbolExceptionRegistry.MethodIdentity terraria_netmessage_senddata = new(terraria_netmessage, "SendData");
	private static readonly SymbolExceptionRegistry.MethodIdentity terraria_netmessage_trysenddata = new(terraria_netmessage, "TrySendData");

	private static readonly SymbolExceptionRegistry exception_registry =
		new SymbolExceptionRegistry()
		   .WhitelistAssembly(tmodloader)
		   .IgnoreParameters(terraria_netmessage_senddata, "number", "number1", "number2", "number3", "number4", "number5", "number6", "number7")
		   .IgnoreParameters(terraria_netmessage_trysenddata, "number", "number1", "number2", "number3", "number4", "number5", "number6", "number7")
		   .IgnoreType(new SymbolExceptionRegistry.TypeIdentity(tmodloader, "Terraria.ModLoader.ModBlockType"))
		   .IgnoreType(new SymbolExceptionRegistry.TypeIdentity(tmodloader, "Terraria.ModLoader.IO.ModBlockEntry"))
		   .IgnoreType(new SymbolExceptionRegistry.TypeIdentity(tmodloader, "Terraria.DataStructures.Point16"))
		   .IgnoreType(new SymbolExceptionRegistry.TypeIdentity(tmodloader, "Terraria.Utils"))
		   .IgnoreType(new SymbolExceptionRegistry.TypeIdentity(tmodloader, "Terraria.Utilities.UnifiedRandom"));

	/// <summary>
	///		Propagates type inference on the compilation, mutating the tracker.
	/// </summary>
	/// <returns>Whether any changes were made.</returns>
	public static bool PropagateOnce(
		Compilation compilation,
		SymbolTracker tracker,
		PropagationTraceGraph trace,
		out int updates
	)
	{
		updates = 0;

		foreach (SyntaxTree? tree in compilation.SyntaxTrees) {
			SemanticModel model = compilation.GetSemanticModel(tree);

			foreach (SyntaxNode? node in tree.GetRoot().DescendantNodes()) {
				if (ShouldSkipNode(node))
					continue;

				var ctx = new Context(model, tracker, exception_registry, trace);

				if (ctx.Exceptions.ShouldSkip(model.GetSymbolInfo(node).Symbol ?? model.GetDeclaredSymbol(node)))
					continue;

				switch (node) {
					// int a = b;
					case VariableDeclaratorSyntax variableDeclaratorSyntax:
						HandleVariableDeclaration(ref ctx, variableDeclaratorSyntax);
						break;

					// a = b;
					case AssignmentExpressionSyntax assignmentExpressionSyntax:
						HandleAssignmentExpression(ref ctx, assignmentExpressionSyntax);
						break;

					// Method(arg);
					case InvocationExpressionSyntax invocationExpressionSyntax:
						HandleInvocationExpression(ref ctx, invocationExpressionSyntax);
						break;

					// return expr;
					case ReturnStatementSyntax returnStatementSyntax:
						HandleReturnStatement(ref ctx, returnStatementSyntax);
						break;
				}

				updates += ctx.Changes;
			}
		}

		return updates > 0;
	}

	private static bool ShouldSkipNode(SyntaxNode? node)
	{
		if (node is null)
			return true;


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

		if (ShouldPropagate(ctx, right, left))
			ctx.Update(right, left, "Variable Declaration RL");

		if (ShouldPropagate(ctx, left, right))
			ctx.Update(left, right, "Variable Declaration LR");
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

		if (ShouldPropagate(ctx, right, left))
			ctx.Update(right, left, "Assignment RL");

		if (ShouldPropagate(ctx, left, right))
			ctx.Update(left, right, "Assignment LR");
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

			ISymbol? arg = ctx.Model.GetSymbolInfo(argExpr).Symbol;
			if (arg is null)
				continue;

			// param -> arg
			if (ShouldPropagate(ctx, param, arg))
				ctx.Update(param, arg, $"Method({method.Name}).{param.Name} -> argument");

			// arg -> param
			if (ShouldPropagate(ctx, arg, param))
				ctx.Update(arg, param, $"argument -> Method({method.Name}).{param.Name}");
		}

		// a = Method(arg);
		if (syntax.Parent is not AssignmentExpressionSyntax retAssign)
			return;

		ISymbol? left = ctx.Model.GetSymbolInfo(retAssign.Left).Symbol;
		if (left is null || !ShouldPropagate(ctx, method, left))
			return;

		ctx.Update(method, left, $"x = Method({method.Name})");
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

		if (!ShouldPropagate(ctx, exprSymbol, method))
			return;

		ctx.Update(exprSymbol, method, $"Method({method.Name}).return = x");
	}

	private static bool ShouldPropagate(Context ctx, ISymbol? from, ISymbol? to)
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

		if (ctx.Exceptions.ShouldIgnore(from) || ctx.Exceptions.ShouldIgnore(to))
			return false;

		IdKind fromKind = ctx.Tracker.GetKind(from);
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