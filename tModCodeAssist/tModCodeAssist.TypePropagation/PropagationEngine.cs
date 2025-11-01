using Microsoft.CodeAnalysis;

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

		return changed;
	}
}