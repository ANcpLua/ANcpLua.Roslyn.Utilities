// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Roslyn.Utilities.Models;

/// <summary>
///     An equatable snapshot of a type declaration for use in incremental source generators —
///     the namespace, declaration keyword, name, generic parameter clause, <c>partial</c> modifier,
///     and the full containing-type chain of a target type.
/// </summary>
/// <remarks>
///     <para>
///         Incremental generators must not cache <see cref="INamedTypeSymbol" /> (it roots entire
///         compilations and uses reference equality). This type captures everything needed to emit a
///         partial declaration for the target — including one nested inside other types — as plain,
///         value-equatable data that is safe to flow through a generator pipeline.
///     </para>
///     <para>
///         The containing-type chain is stored as <b>per-level declarations</b>
///         (<see cref="ContainingTypeInfo" />: keyword, name, generic clause, partial-ness) rather than a
///         flat name string, so emission remains correct when a target is nested inside a generic type,
///         a <c>struct</c>, or a <c>record</c> — cases where a name-only chain would emit a
///         <c>partial class</c> wrapper that fails to compile.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // In the transform stage (cache-safe):
///     var info = TypeDeclarationInfo.From(namedTypeSymbol);
///
///     // In the output stage:
///     var builder = new IndentedStringBuilder();
///     using (info.BeginDeclaration(builder))
///     {
///         builder.AppendLine("public int Generated => 1;");
///     }
///     // Produces (for Deep.Outer&lt;T&gt;.Middle.Inner):
///     // namespace Deep
///     // {
///     //     partial class Outer&lt;T&gt;
///     //     {
///     //         partial struct Middle
///     //         {
///     //             partial class Inner
///     //             {
///     //                 public int Generated => 1;
///     //             }
///     //         }
///     //     }
///     // }
///     </code>
/// </example>
/// <param name="Namespace">The containing namespace, or <c>null</c> for the global namespace.</param>
/// <param name="Keyword">
///     The declaration keyword: <c>"class"</c>, <c>"struct"</c>, <c>"record"</c>, <c>"record struct"</c>,
///     <c>"interface"</c>, <c>"enum"</c>, or <c>"delegate"</c>.
/// </param>
/// <param name="Name">The simple (unqualified, non-generic) type name.</param>
/// <param name="GenericParameterClause">
///     The generic parameter clause (e.g. <c>"&lt;T, U&gt;"</c>), or <c>null</c> if the type is not generic.
/// </param>
/// <param name="IsPartial">Whether any declaration of the type carries the <c>partial</c> modifier.</param>
/// <param name="ContainingTypes">
///     The chain of containing types, outermost first. Empty if the type is not nested.
/// </param>
/// <seealso cref="ContainingTypeInfo" />
/// <seealso cref="IndentedStringBuilder" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct TypeDeclarationInfo(
        string? Namespace,
        string Keyword,
        string Name,
        string? GenericParameterClause,
        bool IsPartial,
        EquatableArray<ContainingTypeInfo> ContainingTypes)
{
    /// <summary>
    ///     Gets a value indicating whether this instance is the default, uninitialized value.
    /// </summary>
    public bool IsDefault => Name is null;

    /// <summary>
    ///     Gets a value indicating whether the type is nested inside another type.
    /// </summary>
    public bool IsNested => !ContainingTypes.IsEmpty;

    /// <summary>
    ///     Gets the declaration name including the generic parameter clause (e.g. <c>"Inner&lt;U&gt;"</c>).
    /// </summary>
    public string DisplayName => GenericParameterClause is null ? Name : Name + GenericParameterClause;

    /// <summary>
    ///     Gets a value indicating whether the target type <b>and every containing type</b> is declared
    ///     <c>partial</c> — the precondition for a generator to add members via a partial declaration.
    /// </summary>
    /// <remarks>
    ///     Use this as the diagnostic gate before emitting: when <c>false</c>, generated code would not
    ///     compile, so report a "type must be partial" diagnostic instead.
    /// </remarks>
    public bool IsFullyPartial
    {
        get
        {
            if (!IsPartial)
                return false;

            foreach (var containing in ContainingTypes.AsImmutableArray())
                if (!containing.IsPartial)
                    return false;

            return true;
        }
    }

    /// <summary>
    ///     Gets a deterministic, collision-free hint name for
    ///     <see cref="SourceProductionContext.AddSource(string, string)" />, derived from the
    ///     namespace, the containing-type chain, and the type name. Nested levels are joined with
    ///     <c>-</c> and generic levels carry a <c>(N)</c> arity marker
    ///     (e.g. <c>"Deep.Outer(1)-Middle-Inner(1).g.cs"</c>).
    /// </summary>
    /// <returns>The hint name, ending in <c>.g.cs</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         The encoding is injective because both markers are characters no C# identifier (and no
    ///         namespace segment) can contain: <c>(N)</c> distinguishes <c>Result&lt;T&gt;</c> from a
    ///         type literally named <c>Result_1</c>, and the <c>-</c> nesting separator distinguishes
    ///         <c>namespace A { class B { class C } }</c> (<c>A.B-C</c>) from
    ///         <c>namespace A.B { class C }</c> (<c>A.B.C</c>). Both characters are accepted by
    ///         Roslyn's hint-name validation. The output is stable across runs, so incremental
    ///         outputs map to the same generated file every time.
    ///     </para>
    /// </remarks>
    public string GetHintName()
    {
        var builder = new StringBuilder(64);

        if (Namespace is not null)
            builder.Append(Namespace).Append('.');

        foreach (var containing in ContainingTypes.AsImmutableArray())
        {
            AppendHintNameLevel(builder, containing.Name, containing.GenericParameterClause);
            builder.Append('-');
        }

        AppendHintNameLevel(builder, Name, GenericParameterClause);

        return builder.Append(".g.cs").ToString();
    }

    private static void AppendHintNameLevel(StringBuilder builder, string name, string? genericParameterClause)
    {
        builder.Append(name);

        if (genericParameterClause is not null)
        {
            var arity = 1;
            foreach (var c in genericParameterClause)
                if (c == ',')
                    arity++;

            builder.Append('(').Append(arity).Append(')');
        }
    }

    /// <summary>
    ///     Creates a <see cref="TypeDeclarationInfo" /> from an <see cref="INamedTypeSymbol" />.
    /// </summary>
    /// <param name="type">The type symbol to snapshot.</param>
    /// <returns>
    ///     A value-equatable snapshot of the type's declaration, safe to cache in an incremental
    ///     generator pipeline.
    /// </returns>
    /// <remarks>
    ///     The <c>partial</c> modifier is detected from the type's declaring syntax references, so this
    ///     factory should be called in the transform stage where syntax is available.
    /// </remarks>
    public static TypeDeclarationInfo From(INamedTypeSymbol type)
    {
        var containingTypes = ImmutableArray<ContainingTypeInfo>.Empty;
        if (type.ContainingType is not null)
        {
            var builder = ImmutableArray.CreateBuilder<ContainingTypeInfo>();
            for (var current = type.ContainingType; current is not null; current = current.ContainingType)
                builder.Add(new ContainingTypeInfo(
                    GetKeyword(current),
                    current.Name,
                    current.GetGenericParameterClause(),
                    IsDeclaredPartial(current)));

            builder.Reverse();
            containingTypes = builder.ToImmutable();
        }

        return new TypeDeclarationInfo(
            type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null,
            GetKeyword(type),
            type.Name,
            type.GetGenericParameterClause(),
            IsDeclaredPartial(type),
            containingTypes.AsEquatableArray());
    }

    /// <summary>
    ///     Writes the opening of the declaration — the namespace block (if any), a <c>partial</c>
    ///     declaration for each containing type, and the <c>partial</c> declaration of the target type —
    ///     and returns a scope that closes all opened blocks when disposed.
    /// </summary>
    /// <param name="builder">The builder to write to.</param>
    /// <returns>
    ///     A <see cref="TypeDeclarationScope" /> that, when disposed, writes the matching closing braces.
    /// </returns>
    /// <remarks>
    ///     Emission uses only the <c>partial</c> modifier plus the captured keyword per level; other
    ///     modifiers (accessibility, <c>readonly</c>, <c>sealed</c>) are merged from the original
    ///     declaration by the compiler and must not be repeated. Only <c>class</c>, <c>struct</c>,
    ///     <c>record</c>, <c>record struct</c>, and <c>interface</c> declarations support partial
    ///     emission; check <see cref="IsFullyPartial" /> first.
    /// </remarks>
    public TypeDeclarationScope BeginDeclaration(IndentedStringBuilder builder)
    {
        var blockCount = 0;

        if (Namespace is not null)
        {
            builder.BeginNamespace(Namespace);
            blockCount++;
        }

        foreach (var containing in ContainingTypes.AsImmutableArray())
        {
            builder.AppendLine($"partial {containing.Keyword} {containing.DisplayName}");
            builder.BeginBlock();
            blockCount++;
        }

        builder.AppendLine($"partial {Keyword} {DisplayName}");
        builder.BeginBlock();
        blockCount++;

        return new TypeDeclarationScope(builder, blockCount);
    }

    private static string GetKeyword(INamedTypeSymbol type)
    {
        return type switch
        {
            { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Struct } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            { TypeKind: TypeKind.Enum } => "enum",
            { TypeKind: TypeKind.Delegate } => "delegate",
            _ => "class",
        };
    }

    private static bool IsDeclaredPartial(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
            if (reference.GetSyntax() is TypeDeclarationSyntax declaration &&
                declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                return true;

        return false;
    }
}

/// <summary>
///     One level of a containing-type chain in a <see cref="TypeDeclarationInfo" />: the declaration
///     keyword, name, generic parameter clause, and <c>partial</c> modifier of a single containing type.
/// </summary>
/// <param name="Keyword">
///     The declaration keyword of the containing type (e.g. <c>"class"</c>, <c>"struct"</c>, <c>"record"</c>).
/// </param>
/// <param name="Name">The simple (unqualified, non-generic) name of the containing type.</param>
/// <param name="GenericParameterClause">
///     The generic parameter clause (e.g. <c>"&lt;T&gt;"</c>), or <c>null</c> if the containing type is not generic.
/// </param>
/// <param name="IsPartial">Whether any declaration of the containing type carries the <c>partial</c> modifier.</param>
/// <seealso cref="TypeDeclarationInfo" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly record struct ContainingTypeInfo(
        string Keyword,
        string Name,
        string? GenericParameterClause,
        bool IsPartial)
{
    /// <summary>
    ///     Gets the declaration name including the generic parameter clause (e.g. <c>"Outer&lt;T&gt;"</c>).
    /// </summary>
    public string DisplayName => GenericParameterClause is null ? Name : Name + GenericParameterClause;
}

/// <summary>
///     A disposable scope returned by <see cref="TypeDeclarationInfo.BeginDeclaration" /> that closes
///     the namespace and all partial-type blocks it opened.
/// </summary>
/// <seealso cref="TypeDeclarationInfo" />
/// <seealso cref="IndentScope" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    readonly struct TypeDeclarationScope : IDisposable
{
    private readonly IndentedStringBuilder? _builder;
    private readonly int _blockCount;

    internal TypeDeclarationScope(IndentedStringBuilder builder, int blockCount)
    {
        _builder = builder;
        _blockCount = blockCount;
    }

    /// <summary>
    ///     Writes the closing brace for every block opened by
    ///     <see cref="TypeDeclarationInfo.BeginDeclaration" />.
    /// </summary>
    public void Dispose()
    {
        if (_builder is null)
            return;

        for (var i = 0; i < _blockCount; i++)
            _builder.EndBlock();
    }
}
