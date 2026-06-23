using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Profiles;

public interface IProfileService
{
    Task<IReadOnlyDictionary<Guid, Profile>> GetProfilesAsync(IEnumerable<Guid> userIds);

    Task<Profile?> GetProfileAsync(Guid userId);
}
