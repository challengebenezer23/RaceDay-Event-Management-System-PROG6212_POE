using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

/// <summary>
/// Enforces RaceDay's server-side session authentication and, optionally,
/// role-based access. Reads the UserId/Role the AuthController wrote into
/// <see cref="ISession"/> at login time:
/// <list type="bullet">
/// <item>No UserId in session -&gt; 401 Unauthorized.</item>
/// <item>One or more roles were passed to the attribute and the session's
/// Role is not among them -&gt; 403 Forbidden.</item>
/// </list>
/// Usage: <c>[SessionAuthorize]</c> for "any authenticated user", or
/// <c>[SessionAuthorize(RaceDayRoles.Organiser)]</c> to restrict to one role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public SessionAuthorizeAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles ?? Array.Empty<string>();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);

        if (userId is null)
        {
            context.Result = new ObjectResult(new { message = "You must be logged in to access this resource." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (_allowedRoles.Length > 0)
        {
            var role = session.GetString(SessionKeys.Role);
            if (role is null || !_allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new
                {
                    message = $"This action requires one of the following roles: {string.Join(", ", _allowedRoles)}."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
