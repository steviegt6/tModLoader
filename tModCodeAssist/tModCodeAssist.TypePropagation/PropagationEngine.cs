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
	private ref struct Context
	{
		public required SemanticModel Model { get; init; }

		public required SymbolTracker Tracker { get; init; }

		public int Changes { get; set; }

		public void Update(ISymbol symbol, IdKind kind)
		{
			if (kind == IdKind.Unknown)
				return;

			if (Tracker.TryUpdate(symbol, kind))
				Changes++;
		}
	}

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
				var context = new Context { Model = model, Tracker = tracker };

				switch (node) {
					// int a = b;
					case VariableDeclaratorSyntax variableDeclaratorSyntax:
						HandleVariableDeclaration(context, variableDeclaratorSyntax);
						break;

					// a = b;
					case AssignmentExpressionSyntax assignmentExpressionSyntax:
						HandleAssignmentExpression(context, assignmentExpressionSyntax);
						break;

					// Method(arg);
					case InvocationExpressionSyntax invocationExpressionSyntax:
						HandleInvocationExpression(context, invocationExpressionSyntax);
						break;

					// return expr;
					case ReturnStatementSyntax returnStatementSyntax:
						HandleReturnStatement(context, returnStatementSyntax);
						break;
				}

				updates += context.Changes;
			}
		}

		return updates > 0;
	}

	private static void HandleVariableDeclaration(
		Context ctx,
		VariableDeclaratorSyntax syntax
	)
	{
		if (syntax.Initializer is null)
			return;

		ISymbol? left = ctx.Model.GetDeclaredSymbol(syntax);
		ISymbol? right = ctx.Model.GetSymbolInfo(syntax.Initializer.Value).Symbol;
		if (left is null || right is null)
			return;

		IdKind kind = ctx.Tracker.GetKind(right);
		ctx.Update(left, kind);
	}

	private static void HandleAssignmentExpression(
		Context ctx,
		AssignmentExpressionSyntax syntax
	)
	{
		ISymbol? left = ctx.Model.GetSymbolInfo(syntax.Left).Symbol;
		ISymbol? right = ctx.Model.GetSymbolInfo(syntax.Right).Symbol;
		if (left is null || right is null)
			return;

		IdKind kind = ctx.Tracker.GetKind(right);
		ctx.Update(left, kind);
	}

	private static void HandleInvocationExpression(
		Context ctx,
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
			IdKind paramKind = ctx.Tracker.GetKind(param);
			if (paramKind != IdKind.Unknown)
				ctx.Update(argSymbol, paramKind);

			// arg -> param
			IdKind argKind = ctx.Tracker.GetKind(argSymbol);
			if (argKind != IdKind.Unknown)
				ctx.Update(param, argKind);
		}

		// a = Method(arg);
		if (syntax.Parent is not AssignmentExpressionSyntax retAssign)
			return;

		ISymbol? left = ctx.Model.GetSymbolInfo(retAssign.Left).Symbol;
		if (left is null)
			return;

		IdKind retKind = ctx.Tracker.GetKind(method);
		if (retKind != IdKind.Unknown)
			ctx.Update(left, retKind);
	}

	private static void HandleReturnStatement(
		Context ctx,
		ReturnStatementSyntax syntax
	)
	{
		if (syntax.Expression is not { } expr)
			return;

		if (ctx.Model.GetSymbolInfo(expr).Symbol is not { } exprSymbol)
			return;

		IdKind exprKind = ctx.Tracker.GetKind(exprSymbol);
		if (exprKind == IdKind.Unknown)
			return;

		if (ctx.Model.GetEnclosingSymbol(expr.SpanStart) is IMethodSymbol method)
			ctx.Update(method, exprKind);
	}
}