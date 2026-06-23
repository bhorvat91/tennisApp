using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Leagues;

public interface ILeagueService
{
    Task<IReadOnlyList<League>> GetLeaguesAsync(Guid clubId);

    Task<League?> GetLeagueAsync(Guid leagueId);

    Task<League?> CreateLeagueAsync(Guid clubId, string name, string? description, string? season, Guid userId, int pointsPerWin = 2, int pointsPerLoss = 1);

    Task<bool> UpdateLeagueStatusAsync(Guid leagueId, string status);

    Task<bool> DeleteLeagueAsync(Guid leagueId);

    Task<IReadOnlyList<LeaguePlayer>> GetLeaguePlayersAsync(Guid leagueId);

    Task<LeaguePlayer?> GetLeaguePlayerAsync(Guid leagueId, Guid userId);

    Task<LeaguePlayer?> JoinLeagueAsync(Guid leagueId, Guid userId);

    Task<bool> WithdrawFromLeagueAsync(Guid leagueId, Guid userId);

    Task<IReadOnlyList<LeagueMatch>> GetMatchesAsync(Guid leagueId);

    Task<LeagueMatch?> CreateMatchAsync(Guid leagueId, Guid player1Id, Guid player2Id, DateOnly? scheduledDate);

    Task<IReadOnlyList<LeagueMatchResult>> GetMatchResultsAsync(Guid leagueId);

    Task<LeagueMatchResult?> EnterResultAsync(Guid matchId, Guid winnerId, string? score, Guid enteredBy);
}
