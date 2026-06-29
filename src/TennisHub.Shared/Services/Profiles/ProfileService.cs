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

    public async Task<bool> UpdateProfileAsync(Guid userId, string? fullName, string? phone, Guid? defaultClubId, string? avatarUrl, string? racket)
    {
        var options = new QueryOptions
        {
            Returning = QueryOptions.ReturnType.Representation
        };

        var query = _client.From<Profile>()
            .Where(x => x.Id == userId)
            .Set(x => x.FullName, fullName)!
            .Set(x => x.Phone, phone)!
            .Set(x => x.DefaultClubId, defaultClubId)!
            .Set(x => x.AvatarUrl, avatarUrl)!
            .Set(x => x.Racket, racket);

        var result = await query.Update(options);
        return result.Models.Count > 0;
    }
}
