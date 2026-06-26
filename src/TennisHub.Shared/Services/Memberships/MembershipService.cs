using Supabase;
using Supabase.Postgrest;
using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Memberships;

public class MembershipService(Supabase.Client client) : IMembershipService
{
    private readonly Supabase.Client _client = client;

    public async Task<bool> RequestJoinAsync(Guid clubId, Guid userId)
    {
        var membership = new ClubMembership
        {
            ClubId = clubId,
            UserId = userId,
            RoleValue = "member",
            StatusValue = "pending"
        };

        var options = new QueryOptions
        {
            Returning = QueryOptions.ReturnType.Representation
        };

        var result = await _client.From<ClubMembership>().Insert(membership, options);
        return result.Model is not null;
    }

    public async Task<ClubMembership?> GetMembershipAsync(Guid clubId, Guid userId)
    {
        return await _client.From<ClubMembership>()
            .Where(x => x.ClubId == clubId && x.UserId == userId)
            .Single();
    }

    public async Task<IReadOnlyList<ClubMembership>> GetPendingMembershipsAsync(Guid clubId)
    {
        var response = await _client.From<ClubMembership>()
            .Where(x => x.ClubId == clubId && x.StatusValue == "pending")
            .Order(x => x.JoinedAt, Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IReadOnlyList<ClubMembership>> GetApprovedMembershipsAsync(Guid clubId)
    {
        var response = await _client.From<ClubMembership>()
            .Where(x => x.ClubId == clubId && x.StatusValue == "approved")
            .Order(x => x.JoinedAt, Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<bool> UpdateMembershipStatusAsync(ClubMembership membership, string status)
    {
        membership.StatusValue = status;

        var options = new QueryOptions
        {
            Returning = QueryOptions.ReturnType.Representation
        };

        var result = await _client.From<ClubMembership>()
            .Where(x => x.Id == membership.Id)
            .Set(x => x.StatusValue, status)
            .Update(options);

        return result.Models.Count > 0;
    }

    public async Task<bool> UpdateMembershipDetailsAsync(ClubMembership membership, string membershipType, bool feePaid, bool canReserve)
    {
        var options = new QueryOptions
        {
            Returning = QueryOptions.ReturnType.Representation
        };

        var result = await _client.From<ClubMembership>()
            .Where(x => x.Id == membership.Id)
            .Set(x => x.MembershipTypeValue, membershipType)
            .Set(x => x.FeePaid, feePaid)
            .Set(x => x.CanReserve, canReserve)
            .Update(options);

        return result.Models.Count > 0;
    }
}
