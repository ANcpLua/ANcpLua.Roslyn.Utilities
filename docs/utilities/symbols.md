# Symbol Extensions

Extension methods for Roslyn symbol types.

## SymbolExtensions

General symbol utilities:

```csharp
// Equality
symbol.IsEqualTo(other)              // SymbolEqualityComparer.Default

// Names
symbol.GetFullyQualifiedName()       // "global::Namespace.Type"
symbol.GetMetadataName()             // "Namespace.Type"
symbol.GetNamespaceName()            // "Namespace"

// Attributes
symbol.HasAttribute("Full.Name")
symbol.HasAttribute(typeSymbol)
symbol.GetAttribute("Full.Name")

// Accessibility
symbol.IsVisibleOutsideOfAssembly()

// Characteristics
symbol.IsOperator()
symbol.IsConst()
symbol.IsTopLevelStatement(ct)

// Members
symbol.GetAllMembers()               // Including inherited
symbol.GetMethod(name)
symbol.GetProperty(name)
symbol.GetSymbolType()               // Type of field/property/parameter

// Interface implementation
symbol.ExplicitlyImplements(interfaceMethod)
symbol.ExplicitOrImplicitInterfaceImplementations()
symbol.ExplicitInterfaceImplementations()

// Type parameters
symbol.GetAllTypeParameters()
symbol.GetTypeParameters()
symbol.GetTypeArguments()
symbol.GetAllTypeArguments()

// Overrides
symbol.GetOverriddenMember()
```

## TypeSymbolExtensions

Type-specific utilities:

```csharp
// Inheritance
type.InheritsFrom(baseType)
type.IsOrInheritsFrom(expectedType)

// Interfaces
type.Implements(interfaceType)
type.IsOrImplements(interfaceType)
type.GetAllInterfacesIncludingThis()

// Attributes
type.HasAttribute(attributeType, inherits: true)
type.GetAttributes(attributeType)
type.GetAttribute(attributeType)

// Special types
type.IsObject()
type.IsString()
type.IsChar()
type.IsInt32() / IsInt64() / IsByte() / ...
type.IsBoolean()
type.IsDateTime()
type.IsSingle() / IsDouble() / IsDecimal()

// Enums
type.IsEnumeration()
type.GetEnumerationType()

// Numbers
type.IsNumberType()

// Nullability
type.GetUnderlyingNullableTypeOrSelf()

// Test detection
type.IsUnitTestClass()               // xUnit, NUnit, MSTest

// Potential static
type.IsPotentialStatic(ct)

// Span/Memory
type.IsSpanType()
type.IsMemoryType()

// Task
type.IsTaskType()                    // Task, ValueTask

// Collections
type.IsEnumerableType()
type.GetElementType()
```

## MethodSymbolExtensions

Method-specific utilities:

```csharp
// Interface implementation
method.IsInterfaceImplementation()
method.GetImplementingInterfaceSymbol()

// Overrides
method.IsOrOverrideMethod(baseMethod)
method.OverridesMethod(baseMethod)
```

## NamespaceSymbolExtensions

Namespace utilities:

```csharp
// Zero-allocation namespace matching
ns.IsNamespace(new[] { "System", "Collections", "Generic" })

// Enumeration
ns.GetAllTypes()                     // Recursive
ns.GetAllNamespaces()
ns.GetPublicTypes()
```

## Examples

### Find All Disposable Types

```csharp
var disposableContext = new DisposableContext(compilation);

foreach (var type in assembly.GetTypesRecursive())
{
    if (disposableContext.IsDisposable(type) &&
        !type.HasAttribute("GeneratedCodeAttribute"))
    {
        // Process disposable type
    }
}
```

### Check Method Override Chain

```csharp
var current = method;
while (current != null)
{
    if (current.HasAttribute("ObsoleteAttribute"))
    {
        // Found obsolete in override chain
        break;
    }
    current = current.GetOverriddenMember() as IMethodSymbol;
}
```

### Get All Interface Implementations

```csharp
foreach (var impl in method.ExplicitOrImplicitInterfaceImplementations())
{
    var interfaceType = impl.ContainingType;
    // Process each interface this method implements
}
```
