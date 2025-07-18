using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

public class TenantRouteTransformer : DynamicRouteValueTransformer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantRouteTransformer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

   
    public override ValueTask<RouteValueDictionary?> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        var action = values["action"]?.ToString()?.ToLower();
        var tenantUrl = values["tenantUrl"]?.ToString();

        if (action == "login" || action == "signup" || tenantUrl == "home")
            return new ValueTask<RouteValueDictionary?>(values);

        if (string.IsNullOrEmpty(tenantUrl))
        {
            tenantUrl = _httpContextAccessor.HttpContext?.Session.GetString("TenantUrl");
            if (string.IsNullOrEmpty(tenantUrl))
            {
                httpContext.Response.Redirect("/Home/Login");
                return new ValueTask<RouteValueDictionary?>((RouteValueDictionary?)null); // ✅ FIXED
            }

            values["tenantUrl"] = tenantUrl;
        }

        //string controllerGuess = tenantUrl?.ToString();
        //if (!values.ContainsKey("controller"))
        //    values["controller"] = controllerGuess;
        if (!values.ContainsKey("controller") || string.IsNullOrEmpty(values["controller"]?.ToString()))
        {
            values["controller"] = ResolveControllerFromAction(action) ?? "Career"; // fallback
        }
        return new ValueTask<RouteValueDictionary?>(values);
    }
    private string? ResolveControllerFromAction(string? action)
    {
        if (string.IsNullOrEmpty(action)) return null;

        var controllerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(Controller).IsAssignableFrom(t) && t.Name.EndsWith("Controller"));

        foreach (var controllerType in controllerTypes)
        {
            var methods = controllerType.GetMethods()
                .Where(m => m.DeclaringType == controllerType && m.IsPublic && !m.IsStatic && m.ReturnType.IsSubclassOf(typeof(ActionResult)));

            if (methods.Any(m => m.Name.Equals(action, StringComparison.OrdinalIgnoreCase)))
            {
                return controllerType.Name.Replace("Controller", ""); // Return controller name without "Controller"
            }
        }

        return null;
    }

}
