using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

/// <summary>
/// Provides cached type symbols and helper methods for analyzing ASP.NET Core and legacy Web API code patterns.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    sealed class AspNetContext
{
    // ASP.NET Core MVC - used internally
    private INamedTypeSymbol? ControllerBase { get; }
    private INamedTypeSymbol? ActionResultInterface { get; }
    private INamedTypeSymbol? ActionResult { get; }
    private INamedTypeSymbol? ActionResultOfT { get; }
    private INamedTypeSymbol? NonActionAttribute { get; }
    private INamedTypeSymbol? NonControllerAttribute { get; }
    private INamedTypeSymbol? HttpMethodAttribute { get; }
    private INamedTypeSymbol? RouteAttribute { get; }
    private INamedTypeSymbol? ApiControllerAttribute { get; }
    private INamedTypeSymbol? ControllerAttribute { get; }
    private INamedTypeSymbol? FromBodyAttribute { get; }
    private INamedTypeSymbol? FromQueryAttribute { get; }
    private INamedTypeSymbol? FromRouteAttribute { get; }
    private INamedTypeSymbol? FromServicesAttribute { get; }
    private INamedTypeSymbol? FromHeaderAttribute { get; }
    private INamedTypeSymbol? FromFormAttribute { get; }

    // ASP.NET Core Minimal APIs - used internally
    private INamedTypeSymbol? ResultInterface { get; }

    // ASP.NET Core Common - used internally
    private INamedTypeSymbol? HttpContext { get; }
    private INamedTypeSymbol? FormFileInterface { get; }

    // Legacy Web API - used internally
    private INamedTypeSymbol? ApiController { get; }
    private INamedTypeSymbol? HttpControllerInterface { get; }
    private INamedTypeSymbol? HttpActionResultInterface { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetContext"/> class by resolving ASP.NET type symbols from the compilation.
    /// </summary>
    /// <param name="compilation">The compilation to resolve type symbols from.</param>
    public AspNetContext(Compilation compilation)
    {
        // ASP.NET Core MVC
        ControllerBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
        ActionResultInterface = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.IActionResult");
        ActionResult = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ActionResult");
        ActionResultOfT = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ActionResult`1");
        NonActionAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.NonActionAttribute");
        NonControllerAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.NonControllerAttribute");
        HttpMethodAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute");
        RouteAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.RouteAttribute");
        ApiControllerAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ApiControllerAttribute");
        ControllerAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerAttribute");
        FromBodyAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromBodyAttribute");
        FromQueryAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromQueryAttribute");
        FromRouteAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromRouteAttribute");
        FromServicesAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromServicesAttribute");
        FromHeaderAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromHeaderAttribute");
        FromFormAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromFormAttribute");

        // ASP.NET Core Minimal APIs
        ResultInterface = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IResult");

        // ASP.NET Core Common
        HttpContext = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext");
        FormFileInterface = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IFormFile");

        // Legacy Web API
        ApiController = compilation.GetTypeByMetadataName("System.Web.Http.ApiController");
        HttpControllerInterface = compilation.GetTypeByMetadataName("System.Web.Http.Controllers.IHttpController");
        HttpActionResultInterface = compilation.GetTypeByMetadataName("System.Web.Http.IHttpActionResult");
    }

    /// <summary>
    /// Determines whether the specified type is an ASP.NET controller.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is a controller; otherwise, <c>false</c>.</returns>
    public bool IsController(INamedTypeSymbol type)
    {
        if (!IsValidControllerCandidate(type))
            return false;

        if (HasNonControllerAttribute(type))
            return false;

        return HasControllerAttribute(type) || InheritsFromControllerBase(type) || IsLegacyController(type);
    }

    /// <summary>
    /// Determines whether the specified type is an API controller (has [ApiController] attribute).
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is an API controller; otherwise, <c>false</c>.</returns>
    public bool IsApiController(INamedTypeSymbol type)
    {
        if (!IsController(type))
            return false;

        return HasApiControllerAttribute(type) || BaseTypeHasApiControllerAttribute(type);
    }

    /// <summary>
    /// Determines whether the specified method is a controller action.
    /// </summary>
    /// <param name="method">The method symbol to check.</param>
    /// <returns><c>true</c> if the method is an action; otherwise, <c>false</c>.</returns>
    public bool IsAction(IMethodSymbol method)
    {
        if (method.MethodKind is not MethodKind.Ordinary)
            return false;

        if (method.IsStatic)
            return false;

        if (method.DeclaredAccessibility is not Accessibility.Public)
            return false;

        if (NonActionAttribute is not null && method.HasAttribute(NonActionAttribute))
            return false;

        return method.ContainingType is { } containingType && IsController(containingType);
    }

    /// <summary>
    /// Determines whether the specified method has an HTTP method attribute (HttpGet, HttpPost, etc.).
    /// </summary>
    /// <param name="method">The method symbol to check.</param>
    /// <returns><c>true</c> if the method has an HTTP method attribute; otherwise, <c>false</c>.</returns>
    public bool HasHttpMethodAttribute(IMethodSymbol method)
    {
        if (HttpMethodAttribute is null)
            return false;

        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass is not null && attr.AttributeClass.InheritsFrom(HttpMethodAttribute))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified symbol has a [Route] attribute.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns><c>true</c> if the symbol has a route attribute; otherwise, <c>false</c>.</returns>
    public bool HasRouteAttribute(ISymbol symbol) =>
        RouteAttribute is not null && symbol.HasAttribute(RouteAttribute);

    /// <summary>
    /// Determines whether the specified type is an action result type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is an action result; otherwise, <c>false</c>.</returns>
    public bool IsActionResult(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        return IsMvcActionResult(type) || IsMinimalApiResult(type) || IsLegacyActionResult(type);
    }

    /// <summary>
    /// Determines whether the specified parameter has a binding source attribute.
    /// </summary>
    /// <param name="parameter">The parameter symbol to check.</param>
    /// <returns><c>true</c> if the parameter has a binding attribute; otherwise, <c>false</c>.</returns>
    public bool HasBindingAttribute(IParameterSymbol parameter)
    {
        foreach (var attr in parameter.GetAttributes())
        {
            if (IsBindingSourceAttribute(attr.AttributeClass))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified parameter has a [FromBody] attribute.
    /// </summary>
    /// <param name="parameter">The parameter symbol to check.</param>
    /// <returns><c>true</c> if the parameter is from body; otherwise, <c>false</c>.</returns>
    public bool IsFromBody(IParameterSymbol parameter) =>
        FromBodyAttribute is not null && parameter.HasAttribute(FromBodyAttribute);

    /// <summary>
    /// Determines whether the specified parameter has a [FromServices] attribute.
    /// </summary>
    /// <param name="parameter">The parameter symbol to check.</param>
    /// <returns><c>true</c> if the parameter is from services; otherwise, <c>false</c>.</returns>
    public bool IsFromServices(IParameterSymbol parameter) =>
        FromServicesAttribute is not null && parameter.HasAttribute(FromServicesAttribute);

    /// <summary>
    /// Determines whether the specified type is the HttpContext type.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is HttpContext; otherwise, <c>false</c>.</returns>
    public bool IsHttpContextType(ITypeSymbol? type) =>
        HttpContext is not null && type.IsEqualTo(HttpContext);

    /// <summary>
    /// Determines whether the specified type is or implements IFormFile.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is a form file; otherwise, <c>false</c>.</returns>
    public bool IsFormFile(ITypeSymbol? type) =>
        type is not null && FormFileInterface is not null && type.IsOrImplements(FormFileInterface);

    // Helper methods to reduce cognitive complexity

    private static bool IsValidControllerCandidate(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Class &&
        type is { IsAbstract: false, DeclaredAccessibility: Accessibility.Public };

    private bool HasNonControllerAttribute(ISymbol type) =>
        NonControllerAttribute is not null && type.HasAttribute(NonControllerAttribute);

    private bool HasControllerAttribute(ISymbol type) =>
        ControllerAttribute is not null && type.HasAttribute(ControllerAttribute);

    private bool InheritsFromControllerBase(ITypeSymbol type) =>
        ControllerBase is not null && type.InheritsFrom(ControllerBase);

    private bool IsLegacyController(ITypeSymbol type)
    {
        if (ApiController is not null && type.InheritsFrom(ApiController))
            return true;

        if (HttpControllerInterface is not null && type.Implements(HttpControllerInterface))
            return type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private bool HasApiControllerAttribute(ISymbol type) =>
        ApiControllerAttribute is not null && type.HasAttribute(ApiControllerAttribute);

    private bool BaseTypeHasApiControllerAttribute(ITypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (HasApiControllerAttribute(baseType))
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }

    private bool IsMvcActionResult(ITypeSymbol type)
    {
        if (ActionResultInterface is not null && type.IsOrImplements(ActionResultInterface))
            return true;

        if (ActionResult is not null && type.IsOrInheritsFrom(ActionResult))
            return true;

        if (ActionResultOfT is not null && type is INamedTypeSymbol named &&
            SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, ActionResultOfT))
            return true;

        return false;
    }

    private bool IsMinimalApiResult(ITypeSymbol type) =>
        ResultInterface is not null && type.IsOrImplements(ResultInterface);

    private bool IsLegacyActionResult(ITypeSymbol type) =>
        HttpActionResultInterface is not null && type.IsOrImplements(HttpActionResultInterface);

    private bool IsBindingSourceAttribute(INamedTypeSymbol? attrType)
    {
        if (attrType is null)
            return false;

        return (FromBodyAttribute is not null && attrType.IsEqualTo(FromBodyAttribute)) ||
               (FromQueryAttribute is not null && attrType.IsEqualTo(FromQueryAttribute)) ||
               (FromRouteAttribute is not null && attrType.IsEqualTo(FromRouteAttribute)) ||
               (FromServicesAttribute is not null && attrType.IsEqualTo(FromServicesAttribute)) ||
               (FromHeaderAttribute is not null && attrType.IsEqualTo(FromHeaderAttribute)) ||
               (FromFormAttribute is not null && attrType.IsEqualTo(FromFormAttribute));
    }
}