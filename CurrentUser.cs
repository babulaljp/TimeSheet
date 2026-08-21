using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string UserName { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
   
    public CurrentUser(IHttpContextAccessor http)
    {
        _http = http; 
    }


    private ClaimsPrincipal? User => _http?.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string UserId => User?.FindFirst("UserId")?.Value ?? _http?.HttpContext?.Request.Cookies["userid"];

    public string UserName => User?.Identity?.Name ?? string.Empty;
}