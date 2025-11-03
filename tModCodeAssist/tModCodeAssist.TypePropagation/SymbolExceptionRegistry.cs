using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace tModCodeAssist.TypePropagation;

/// <summary>
///		Determines what symbols to propagate types through.
/// </summary>
public sealed class SymbolExceptionRegistry
{
	public readonly record struct AssemblyIdentity(string AssemblyName);

	public readonly record struct TypeIdentity(AssemblyIdentity DeclaringAssembly, string TypeName);

	public readonly record struct MethodIdentity(TypeIdentity DeclaringType, string MethodName);

	public readonly record struct ParameterIdentity(MethodIdentity DeclaringMethod, string ParameterName);

	private readonly HashSet<string> whitelistedAssemblies = [];
	private readonly HashSet<TypeIdentity> ignoredTypes = [];
	private readonly HashSet<MethodIdentity> ignoredMethods = [];
	private readonly HashSet<ParameterIdentity> ignoredParameters = [];
	private readonly HashSet<TypeIdentity> ignoredTypeSyntaxes = [];
	private readonly HashSet<MethodIdentity> ignoredMethodSyntaxes = [];

	/// <summary>
	///		Determines whether a given symbol should be skipped over when
	///		iterating over syntaxes for type propagation.
	/// </summary>
	public bool ShouldSkip(ISymbol? symbol)
	{
		if (symbol?.ContainingAssembly is not { } containingAssembly)
			return false;

		if (!whitelistedAssemblies.Contains(symbol.ContainingAssembly.Name))
			return true;

		if (symbol.ContainingType is not { } containingType)
			return false;

		var typeId = new TypeIdentity(
			new AssemblyIdentity(containingAssembly.Name),
			containingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
		);

		if (ignoredTypeSyntaxes.Contains(typeId))
			return true;

		if (symbol.ContainingSymbol is not IMethodSymbol method)
			return false;

		var methodId = new MethodIdentity(typeId, method.Name);
		if (ignoredMethodSyntaxes.Contains(methodId))
			return true;

		return false;
	}

	/// <summary>
	///		Determines whether a given symbol should be considered for type
	///		propagation.
	/// </summary>
	public bool ShouldIgnore(ISymbol? symbol)
	{
		if (symbol?.ContainingAssembly is not { } containingAssembly)
			return true;

		if (!whitelistedAssemblies.Contains(symbol.ContainingAssembly.Name))
			return true;

		if (symbol.ContainingType is not { } containingType)
			return false;

		var typeId = new TypeIdentity(
			new AssemblyIdentity(containingAssembly.Name),
			containingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
		);

		if (ignoredTypes.Contains(typeId))
			return true;

		if (symbol.ContainingSymbol is not IMethodSymbol method)
			return false;

		var methodId = new MethodIdentity(typeId, method.Name);
		if (ignoredMethods.Contains(methodId))
			return true;

		if (symbol is not IParameterSymbol param)
			return false;

		var paramId = new ParameterIdentity(methodId, param.Name);
		return ignoredParameters.Contains(paramId);
	}

	public SymbolExceptionRegistry IgnoreTypeSyntax(AssemblyIdentity assembly, string typeName) =>
		IgnoreTypeSyntax(new TypeIdentity(assembly, typeName));

	public SymbolExceptionRegistry IgnoreTypeSyntax(TypeIdentity type)
	{
		ignoredTypeSyntaxes.Add(type);
		return this;
	}

	public SymbolExceptionRegistry IgnoreMethodSyntax(TypeIdentity type, string methodName) =>
		IgnoreMethodSyntax(new MethodIdentity(type, methodName));

	public SymbolExceptionRegistry IgnoreMethodSyntax(MethodIdentity method)
	{
		ignoredMethodSyntaxes.Add(method);
		return this;
	}

	public SymbolExceptionRegistry WhitelistAssembly(AssemblyIdentity assembly)
	{
		whitelistedAssemblies.Add(assembly.AssemblyName);
		return this;
	}

	public SymbolExceptionRegistry IgnoreType(AssemblyIdentity assembly, string typeName) =>
		IgnoreType(new TypeIdentity(assembly, typeName));

	public SymbolExceptionRegistry IgnoreType(TypeIdentity type)
	{
		ignoredTypes.Add(type);
		return this;
	}

	public SymbolExceptionRegistry IgnoreMethod(TypeIdentity type, string methodName) =>
		IgnoreMethod(new MethodIdentity(type, methodName));

	public SymbolExceptionRegistry IgnoreMethod(MethodIdentity method)
	{
		ignoredMethods.Add(method);
		return this;
	}

	public SymbolExceptionRegistry IgnoreParameters(MethodIdentity method, params string[] parameterNames)
	{
		foreach (string param in parameterNames)
			ignoredParameters.Add(new ParameterIdentity(method, param));

		return this;
	}

	public SymbolExceptionRegistry IgnoreParameters(params ParameterIdentity[] parameters)
	{
		foreach (ParameterIdentity param in parameters)
			ignoredParameters.Add(param);

		return this;
	}
}