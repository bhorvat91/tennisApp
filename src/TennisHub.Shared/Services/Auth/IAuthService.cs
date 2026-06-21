using Supabase.Gotrue;

namespace TennisHub.Shared.Services.Auth;

public interface IAuthService
{
    event Action? AuthStateChanged;

    User? CurrentUser { get; }

    Task InitializeAsync();

    Task<AuthOperationResult> SignUpAsync(string email, string password, string fullName);

    Task<AuthOperationResult> SignInAsync(string email, string password);

    Task SignOutAsync();
}
