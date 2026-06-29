using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TennisHub.Shared.Services.Auth;

public class SupabaseAuthenticationStateProvider(IAuthService authService) : AuthenticationStateProvider
{
    private readonly IAuthService _authService = authService;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _authService.InitializeAsync();

        var user = _authService.CurrentUser;
        if (user is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (user.UserMetadata is not null
            && user.UserMetadata.TryGetValue("full_name", out var fullNameObject)
            && fullNameObject is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, fullNameObject.ToString() ?? user.Email ?? user.Id ?? string.Empty));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Name, user.Email ?? user.Id ?? string.Empty));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "supabase");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthenticationStateChangedAsync()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
