using System.Text.Json;
using Supabase.Gotrue;
using Microsoft.JSInterop;

namespace TennisHub.Shared.Services.Auth;

public class AuthService(Supabase.Client client, IJSRuntime jsRuntime) : IAuthService
{
    private const string SessionStorageKey = "tennishub.auth.session";

    private readonly Supabase.Client _client = client;
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private bool _initialized;

    public event Action? AuthStateChanged;

    public User? CurrentUser => _client.Auth.CurrentUser;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _client.InitializeAsync();
        var storedSession = await LoadSessionAsync();

        if (_client.Auth.CurrentSession is null && storedSession is not null)
        {
            await _client.Auth.SetSession(storedSession.AccessToken, storedSession.RefreshToken, true);
        }

        _initialized = true;
        RaiseAuthStateChanged();
    }

    public async Task<AuthOperationResult> SignUpAsync(string email, string password, string fullName)
    {
        try
        {
            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    ["full_name"] = fullName
                }
            };

            await _client.Auth.SignUp(email, password, options);
            await PersistCurrentSessionAsync();
            RaiseAuthStateChanged();

            return new AuthOperationResult(true);
        }
        catch (Exception ex)
        {
            return new AuthOperationResult(false, ex.Message);
        }
    }

    public async Task<AuthOperationResult> SignInAsync(string email, string password)
    {
        try
        {
            await _client.Auth.SignIn(email, password);
            await PersistCurrentSessionAsync();
            RaiseAuthStateChanged();

            return new AuthOperationResult(true);
        }
        catch (Exception ex)
        {
            return new AuthOperationResult(false, ex.Message);
        }
    }

    public async Task SignOutAsync()
    {
        await _client.Auth.SignOut();
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", SessionStorageKey);
        RaiseAuthStateChanged();
    }

    private async Task PersistCurrentSessionAsync()
    {
        var session = _client.Auth.CurrentSession;
        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken) || string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            return;
        }

        var stored = new StoredSession(session.AccessToken, session.RefreshToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SessionStorageKey, JsonSerializer.Serialize(stored));
    }

    private async Task<StoredSession?> LoadSessionAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SessionStorageKey);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<StoredSession>(json);
    }

    private void RaiseAuthStateChanged() => AuthStateChanged?.Invoke();

    private sealed record StoredSession(string AccessToken, string RefreshToken);
}
