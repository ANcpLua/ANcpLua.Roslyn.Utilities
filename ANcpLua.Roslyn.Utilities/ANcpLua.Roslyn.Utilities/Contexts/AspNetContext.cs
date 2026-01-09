using Microsoft.CodeAnalysis;

namespace ANcpLua.Roslyn.Utilities.Contexts;

#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
sealed class AspNetContext
{
    // ASP.NET Core MVC
    public INamedTypeSymbol? ControllerBase { get; }
    public INamedTypeSymbol? Controller { get; }
    public INamedTypeSymbol? IActionResult { get; }
    public INamedTypeSymbol? ActionResult { get; }
    public INamedTypeSymbol? ActionResultOfT { get; }
    public INamedTypeSymbol? NonActionAttribute { get; }
    public INamedTypeSymbol? NonControllerAttribute { get; }
    public INamedTypeSymbol? HttpMethodAttribute { get; }
    public INamedTypeSymbol? RouteAttribute { get; }
    public INamedTypeSymbol? ApiControllerAttribute { get; }
    public INamedTypeSymbol? ControllerAttribute { get; }
    public INamedTypeSymbol? FromBodyAttribute { get; }
    public INamedTypeSymbol? FromQueryAttribute { get; }
    public INamedTypeSymbol? FromRouteAttribute { get; }
    public INamedTypeSymbol? FromServicesAttribute { get; }
    public INamedTypeSymbol? FromHeaderAttribute { get; }
    public INamedTypeSymbol? FromFormAttribute { get; }
    public INamedTypeSymbol? BindPropertyAttribute { get; }
    public INamedTypeSymbol? ModelBinderAttribute { get; }

    // ASP.NET Core Minimal APIs
    public INamedTypeSymbol? IEndpointRouteBuilder { get; }
    public INamedTypeSymbol? IResult { get; }
    public INamedTypeSymbol? Results { get; }
    public INamedTypeSymbol? TypedResults { get; }

    // ASP.NET Core Common
    public INamedTypeSymbol? HttpContext { get; }
    public INamedTypeSymbol? HttpRequest { get; }
    public INamedTypeSymbol? HttpResponse { get; }
    public INamedTypeSymbol? IFormFile { get; }
    public INamedTypeSymbol? CancellationToken { get; }

    // Legacy Web API
    public INamedTypeSymbol? ApiController { get; }
    public INamedTypeSymbol? IHttpController { get; }
    public INamedTypeSymbol? IHttpActionResult { get; }
    public INamedTypeSymbol? HttpResponseMessage { get; }

    public AspNetContext(Compilation compilation)
    {
        // ASP.NET Core MVC
        ControllerBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
        Controller = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controller");
        IActionResult = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.IActionResult");
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
        BindPropertyAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.BindPropertyAttribute");
        ModelBinderAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ModelBinderAttribute");

        // ASP.NET Core Minimal APIs
        IEndpointRouteBuilder = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Routing.IEndpointRouteBuilder");
        IResult = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IResult");
        Results = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Results");
        TypedResults = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.TypedResults");

        // ASP.NET Core Common
        HttpContext = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext");
        HttpRequest = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpRequest");
        HttpResponse = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpResponse");
        IFormFile = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IFormFile");
        CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        // Legacy Web API
        ApiController = compilation.GetTypeByMetadataName("System.Web.Http.ApiController");
        IHttpController = compilation.GetTypeByMetadataName("System.Web.Http.Controllers.IHttpController");
        IHttpActionResult = compilation.GetTypeByMetadataName("System.Web.Http.IHttpActionResult");
        HttpResponseMessage = compilation.GetTypeByMetadataName("System.Net.Http.HttpResponseMessage");
    }

    public bool IsController(INamedTypeSymbol type)
    {
        if (type.TypeKind is not TypeKind.Class)
            return false;

        if (type.IsAbstract)
            return false;

        if (type.DeclaredAccessibility is not Accessibility.Public)
            return false;

        // Check for [NonController] attribute
        if (NonControllerAttribute is not null && type.HasAttribute(NonControllerAttribute))
            return false;

        // Check for [Controller] attribute - makes any class a controller
        if (ControllerAttribute is not null && type.HasAttribute(ControllerAttribute))
            return true;

        // ASP.NET Core: inherits from ControllerBase
        if (ControllerBase is not null && type.InheritsFrom(ControllerBase))
            return true;

        // Legacy Web API: inherits from ApiController or implements IHttpController
        if (ApiController is not null && type.InheritsFrom(ApiController))
            return true;

        if (IHttpController is not null && type.Implements(IHttpController))
        {
            // Legacy convention: name must end with "Controller"
            if (type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool IsApiController(INamedTypeSymbol type)
    {
        if (!IsController(type))
            return false;

        if (ApiControllerAttribute is not null && type.HasAttribute(ApiControllerAttribute))
            return true;

        // Check if any base type has [ApiController]
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (ApiControllerAttribute is not null && baseType.HasAttribute(ApiControllerAttribute))
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }

    public bool IsAction(IMethodSymbol method)
    {
        if (method.MethodKind is not MethodKind.Ordinary)
            return false;

        if (method.IsStatic)
            return false;

        if (method.DeclaredAccessibility is not Accessibility.Public)
            return false;

        // Check for [NonAction] attribute
        if (NonActionAttribute is not null && method.HasAttribute(NonActionAttribute))
            return false;

        // Must be in a controller
        if (method.ContainingType is not INamedTypeSymbol containingType || !IsController(containingType))
            return false;

        return true;
    }

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

    public bool HasRouteAttribute(ISymbol symbol) =>
        RouteAttribute is not null && symbol.HasAttribute(RouteAttribute);

    public bool IsActionResult(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (IActionResult is not null && type.IsOrImplements(IActionResult))
            return true;

        if (ActionResult is not null && type.IsOrInheritsFrom(ActionResult))
            return true;

        if (ActionResultOfT is not null && type is INamedTypeSymbol named &&
            SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, ActionResultOfT))
            return true;

        // Minimal APIs
        if (IResult is not null && type.IsOrImplements(IResult))
            return true;

        // Legacy
        if (IHttpActionResult is not null && type.IsOrImplements(IHttpActionResult))
            return true;

        return false;
    }

    public bool HasBindingAttribute(IParameterSymbol parameter)
    {
        foreach (var attr in parameter.GetAttributes())
        {
            var attrType = attr.AttributeClass;
            if (attrType is null)
                continue;

            if ((FromBodyAttribute is not null && attrType.IsEqualTo(FromBodyAttribute)) ||
                (FromQueryAttribute is not null && attrType.IsEqualTo(FromQueryAttribute)) ||
                (FromRouteAttribute is not null && attrType.IsEqualTo(FromRouteAttribute)) ||
                (FromServicesAttribute is not null && attrType.IsEqualTo(FromServicesAttribute)) ||
                (FromHeaderAttribute is not null && attrType.IsEqualTo(FromHeaderAttribute)) ||
                (FromFormAttribute is not null && attrType.IsEqualTo(FromFormAttribute)))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsFromBody(IParameterSymbol parameter) =>
        FromBodyAttribute is not null && parameter.HasAttribute(FromBodyAttribute);

    public bool IsFromServices(IParameterSymbol parameter) =>
        FromServicesAttribute is not null && parameter.HasAttribute(FromServicesAttribute);

    public bool IsHttpContextType(ITypeSymbol? type) =>
        HttpContext is not null && type.IsEqualTo(HttpContext);

    public bool IsFormFile(ITypeSymbol? type) =>
        type is not null && IFormFile is not null && type.IsOrImplements(IFormFile);
}
