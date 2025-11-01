using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Responsible for determining type propagation.
/// </summary>
public static class PropagationEngine
{
	/// <summary>
	///		Propagates type inference on the compilation, mutating the tracker.
	/// </summary>
	/// <returns>Whether any changes were made.</returns>
	public static bool PropagateOnce(
		Compilation compilation,
		SymbolTracker tracker
	)
	{
		bool changed = false;

		foreach (SyntaxTree? tree in compilation.SyntaxTrees) {
			SemanticModel model = compilation.GetSemanticModel(tree);

			// TODO: Flow/more expression analysis.
			// Method(expr) -> expr : ID? for example
			foreach (SyntaxNode? node in tree.GetRoot().DescendantNodes()) {
				// int a = b;
				if (node is VariableDeclaratorSyntax { Initializer: not null } varDecl) {
					ISymbol left = model.GetDeclaredSymbol(varDecl)!;
					ISymbol? right = model.GetSymbolInfo(varDecl.Initializer.Value).Symbol;

					IdKind kind = tracker.GetKind(right);
					if (kind != IdKind.Unknown)
						changed |= tracker.TryUpdate(left, kind);

					continue;
				}

				// a = b;
				if (node is AssignmentExpressionSyntax assignment) {
					ISymbol? left = model.GetSymbolInfo(assignment.Left).Symbol;
					ISymbol? right = model.GetSymbolInfo(assignment.Right).Symbol;
					if (left is null || right is null)
						continue;

					IdKind kind = tracker.GetKind(right);
					if (kind != IdKind.Unknown)
						changed |= tracker.TryUpdate(left, kind);
				}

				// Method(arg);
				if (node is InvocationExpressionSyntax invocation) {
					if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
						continue;

					SeparatedSyntaxList<ArgumentSyntax> args = invocation.ArgumentList.Arguments;
					for (int i = 0; i < args.Count && i < method.Parameters.Length; i++) {
						IParameterSymbol param = method.Parameters[i];
						ExpressionSyntax argExpr = args[i].Expression;

						ISymbol? argSymbol = model.GetSymbolInfo(argExpr).Symbol;
						if (argSymbol is null) {
							continue;
						}

						// param -> arg
						IdKind paramKind = tracker.GetKind(param);
						if (paramKind != IdKind.Unknown)
							changed |= tracker.TryUpdate(argSymbol, paramKind);

						// arg -> param
						IdKind argKind = tracker.GetKind(argSymbol);
						if (argKind != IdKind.Unknown)
							changed |= tracker.TryUpdate(param, argKind);
					}

					// a = Method(arg);
					if (invocation.Parent is AssignmentExpressionSyntax retAssign) {
						ISymbol? left = model.GetSymbolInfo(retAssign.Left).Symbol;
						if (left is null)
							continue;

						IdKind retKind = tracker.GetKind(method);
						if (retKind != IdKind.Unknown)
							changed |= tracker.TryUpdate(left, retKind);
					}
				}

				// return expr;
				if (node is ReturnStatementSyntax ret) {
					if (ret.Expression is not { } expr)
						continue;

					if (model.GetSymbolInfo(expr).Symbol is not { } exprSymbol)
						continue;

					IdKind exprKind = tracker.GetKind(exprSymbol);
					if (exprKind == IdKind.Unknown)
						continue;

					if (model.GetEnclosingSymbol(expr.SpanStart) is IMethodSymbol method)
						changed |= tracker.TryUpdate(method, exprKind);

					continue;
				}
			}
		}

		return changed;
	}
}