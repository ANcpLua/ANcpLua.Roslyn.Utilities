namespace ANcpLua.Analyzers.AotReflection;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

public sealed class ClassMetadata
{
#if NET6_0_OR_GREATER
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicProperties)]
#endif
    public Type Type { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Namespace { get; set; } = string.Empty;

    public bool IsStatic { get; set; }

    public bool IsSealed { get; set; }

    public bool IsAbstract { get; set; }

    public ClassMetadata? BaseType { get; set; }

    private PropertyMetadata[] _properties = Array.Empty<PropertyMetadata>();

    public PropertyMetadata[] Properties
    {
        get => _properties;
        set
        {
            _properties = value;
            foreach (var property in _properties) property.ClassMetadata = this;
        }
    }

    public MethodMetadata[] Methods { get; set; } = Array.Empty<MethodMetadata>();

    public FieldMetadata[] Fields { get; set; } = Array.Empty<FieldMetadata>();

    public ConstructorMetadata[] Constructors { get; set; } = Array.Empty<ConstructorMetadata>();

    public object? GetPropertyValue(object instance, string name)
    {
        foreach (var property in Properties)
        {
            if (!string.Equals(property.Name, name, StringComparison.Ordinal)) continue;

            return property.Getter is null
                ? throw new InvalidOperationException($"Property '{name}' does not have a getter.")
                : property.Getter(property.IsStatic ? null : instance);
        }

        throw new ArgumentException("Unknown property name.", nameof(name));
    }

    public void SetPropertyValue(object instance, string name, object? value)
    {
        foreach (var property in Properties)
        {
            if (!string.Equals(property.Name, name, StringComparison.Ordinal)) continue;

            if (property.Setter is null)
                throw new InvalidOperationException($"Property '{name}' does not have a setter.");

            property.Setter(property.IsStatic ? null : instance, value);
            return;
        }

        throw new ArgumentException("Unknown property name.", nameof(name));
    }

    public object? InvokeMethod(object? instance, string name, params object?[] args)
    {
        MethodMetadata? arityMatch = null;

        foreach (var method in Methods)
        {
            if (!string.Equals(method.Name, name, StringComparison.Ordinal)) continue;

            if (method.Parameters.Length != args.Length) continue;

            arityMatch ??= method;

            if (!ParametersMatch(method.Parameters, args)) continue;

            return Invoke(method, instance, args);
        }

        // No parameter-type-exact overload; fall back to the first name+arity match.
        if (arityMatch is not null) return Invoke(arityMatch, instance, args);

        throw new ArgumentException("Unknown method name or parameter count.", nameof(name));
    }

    public object CreateInstance(params object?[] args)
    {
        ConstructorMetadata? arityMatch = null;

        foreach (var constructor in Constructors)
        {
            if (constructor.Parameters.Length != args.Length) continue;

            arityMatch ??= constructor;

            if (!ParametersMatch(constructor.Parameters, args)) continue;

            return Create(constructor, args);
        }

        // No parameter-type-exact overload; fall back to the first arity match.
        if (arityMatch is not null) return Create(arityMatch, args);

        throw new ArgumentException("No matching constructor found.", nameof(args));
    }

    private static object? Invoke(MethodMetadata method, object? instance, object?[] args) =>
        method.Invoker is null
            ? throw new InvalidOperationException($"Method '{method.Name}' does not have an invoker.")
            : method.Invoker(method.IsStatic ? null : instance, args);

    private static object Create(ConstructorMetadata constructor, object?[] args) =>
        constructor.Factory is null
            ? throw new InvalidOperationException("Constructor does not have a factory.")
            : constructor.Factory(args);

    // Same-name + same-arity overloads are disambiguated by matching each argument's
    // runtime type against the parameter type; a null argument fits any type that can
    // hold null. Callers with no type-exact overload get the first-arity-match fallback.
    private static bool ParametersMatch(ParameterMetadata[] parameters, object?[] args)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].Type;

            var matches = args[i] switch
            {
                null => !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) is not null,
                var argument => parameterType.IsInstanceOfType(argument),
            };

            if (!matches) return false;
        }

        return true;
    }
}