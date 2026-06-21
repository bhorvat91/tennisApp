using Supabase;
using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Clubs;

public class ClubService(Client client) : IClubService
{
    private readonly Client _client = client;

    public async Task<IReadOnlyList<Club>> GetClubsAsync()
    {
        var response = await _client.From<Club>()
            .Order(x => x.Name, Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<Club?> GetClubByIdAsync(Guid clubId)
    {
        return await _client.From<Club>()
            .Where(x => x.Id == clubId)
            .Single();
    }

    public async Task<Club?> CreateClubAsync(string name, string? address, Guid userId)
    {
        var club = new Club
        {
            Name = name,
            Address = string.IsNullOrWhiteSpace(address) ? null : address,
            CreatedBy = userId
        };

        var result = await _client.From<Club>().Insert(club);
        return result.Model;
    }

    public async Task<IReadOnlyList<ClubMembership>> GetCurrentUserMembershipsAsync(Guid userId)
    {
        var response = await _client.From<ClubMembership>()
            .Where(x => x.UserId == userId)
            .Get();

        return response.Models;
    }
}
