using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Profiles;

public interface IProfileService
{
    Task<IReadOnlyDictionary<Guid, Profile>> GetProfilesAsync(IEnumerable<Guid> userIds);

    Task<Profile?> GetProfileAsync(Guid userId);

    Task<bool> UpdateProfileAsync(Guid userId, string? fullName, string? phone, Guid? defaultClubId, string? avatarUrl, string? racket);
}
