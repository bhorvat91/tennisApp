using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Clubs;

public interface IClubService
{
    Task<IReadOnlyList<Club>> GetClubsAsync();

    Task<Club?> GetClubByIdAsync(Guid clubId);

    Task<Club?> CreateClubAsync(string name, string? address, Guid userId);

    Task<IReadOnlyList<ClubMembership>> GetCurrentUserMembershipsAsync(Guid userId);
}
