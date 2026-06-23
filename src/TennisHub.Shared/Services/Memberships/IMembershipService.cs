using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Memberships;

public interface IMembershipService
{
    Task<bool> RequestJoinAsync(Guid clubId, Guid userId);

    Task<ClubMembership?> GetMembershipAsync(Guid clubId, Guid userId);

    Task<IReadOnlyList<ClubMembership>> GetPendingMembershipsAsync(Guid clubId);

    Task<IReadOnlyList<ClubMembership>> GetApprovedMembershipsAsync(Guid clubId);

    Task<bool> UpdateMembershipStatusAsync(ClubMembership membership, string status);

    Task<bool> UpdateMembershipDetailsAsync(ClubMembership membership, string membershipType, bool feePaid, bool canReserve);
}
