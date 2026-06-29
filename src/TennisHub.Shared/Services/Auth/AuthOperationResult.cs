namespace TennisHub.Shared.Services.Auth;

public record AuthOperationResult(bool Success, string? ErrorMessage = null);
