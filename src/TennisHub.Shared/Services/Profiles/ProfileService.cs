using Supabase.Postgrest;
using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Profiles;

public class ProfileService(Supabase.Client client) : IProfileService
{
    private readonly Supabase.Client _client = client;

    public async Task<IReadOnlyDictionary<Guid, Profile>> GetProfilesAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Select(id => id.ToString()).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, Profile>();
        }

        var response = await _client.From<Profile>()
            .Filter("id", Constants.Operator.In, ids)
            .Get();

        return response.Models.ToDictionary(p => p.Id);
    }

    public async Task<Profile?> GetProfileAsync(Guid userId)
    {
        return await _client.From<Profile>()
            .Where(x => x.Id == userId)
            .Single();
    }
}
