using Supabase.Postgrest;
using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Leagues;

public class LeagueService(Supabase.Client client) : ILeagueService
{
    private readonly Supabase.Client _client = client;

    public async Task<IReadOnlyList<League>> GetLeaguesAsync(Guid clubId)
    {
        var response = await _client.From<League>()
            .Where(x => x.ClubId == clubId)
            .Order(x => x.CreatedAt, Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<League?> GetLeagueAsync(Guid leagueId)
    {
        return await _client.From<League>()
            .Where(x => x.Id == leagueId)
            .Single();
    }

    public async Task<League?> CreateLeagueAsync(Guid clubId, string name, string? description, string? season, Guid userId)
    {
        var league = new League
        {
            ClubId = clubId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Season = string.IsNullOrWhiteSpace(season) ? null : season,
            Status = "active",
            CreatedBy = userId
        };

        var result = await _client.From<League>().Insert(league);
        return result.Model;
    }

    public async Task<bool> UpdateLeagueStatusAsync(Guid leagueId, string status)
    {
        var options = new QueryOptions { Returning = QueryOptions.ReturnType.Representation };
        var result = await _client.From<League>()
            .Where(x => x.Id == leagueId)
            .Set(x => x.Status, status)
            .Update(options);

        return result.Models.Count > 0;
    }

    public async Task<bool> DeleteLeagueAsync(Guid leagueId)
    {
        await _client.From<League>()
            .Where(x => x.Id == leagueId)
            .Delete();

        return true;
    }

    public async Task<IReadOnlyList<LeaguePlayer>> GetLeaguePlayersAsync(Guid leagueId)
    {
        var response = await _client.From<LeaguePlayer>()
            .Where(x => x.LeagueId == leagueId && x.Status == "active")
            .Order(x => x.JoinedAt, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<LeaguePlayer?> GetLeaguePlayerAsync(Guid leagueId, Guid userId)
    {
        return await _client.From<LeaguePlayer>()
            .Where(x => x.LeagueId == leagueId && x.UserId == userId)
            .Single();
    }

    public async Task<LeaguePlayer?> JoinLeagueAsync(Guid leagueId, Guid userId)
    {
        var existing = await GetLeaguePlayerAsync(leagueId, userId);
        if (existing is not null)
        {
            if (existing.Status == "withdrawn")
            {
                var options = new QueryOptions { Returning = QueryOptions.ReturnType.Representation };
                var reactivated = await _client.From<LeaguePlayer>()
                    .Where(x => x.Id == existing.Id)
                    .Set(x => x.Status, "active")
                    .Update(options);
                return reactivated.Model;
            }

            return existing;
        }

        var player = new LeaguePlayer { LeagueId = leagueId, UserId = userId, Status = "active" };
        var result = await _client.From<LeaguePlayer>().Insert(player);
        return result.Model;
    }

    public async Task<bool> WithdrawFromLeagueAsync(Guid leagueId, Guid userId)
    {
        var options = new QueryOptions { Returning = QueryOptions.ReturnType.Representation };
        var result = await _client.From<LeaguePlayer>()
            .Where(x => x.LeagueId == leagueId && x.UserId == userId)
            .Set(x => x.Status, "withdrawn")
            .Update(options);

        return result.Models.Count > 0;
    }

    public async Task<IReadOnlyList<LeagueMatch>> GetMatchesAsync(Guid leagueId)
    {
        var response = await _client.From<LeagueMatch>()
            .Where(x => x.LeagueId == leagueId)
            .Order(x => x.CreatedAt, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<LeagueMatch?> CreateMatchAsync(Guid leagueId, Guid player1Id, Guid player2Id, DateOnly? scheduledDate)
    {
        var match = new LeagueMatch
        {
            LeagueId = leagueId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            ScheduledDate = scheduledDate,
            Status = "pending"
        };

        var result = await _client.From<LeagueMatch>().Insert(match);
        return result.Model;
    }

    public async Task<IReadOnlyList<LeagueMatchResult>> GetMatchResultsAsync(Guid leagueId)
    {
        var matches = await GetMatchesAsync(leagueId);
        if (matches.Count == 0)
        {
            return [];
        }

        var matchIds = matches.Select(m => m.Id.ToString()).ToList();
        var response = await _client.From<LeagueMatchResult>()
            .Filter("match_id", Constants.Operator.In, matchIds)
            .Get();

        return response.Models;
    }

    public async Task<LeagueMatchResult?> EnterResultAsync(Guid matchId, Guid winnerId, string? score, Guid enteredBy)
    {
        var existing = await _client.From<LeagueMatchResult>()
            .Where(x => x.MatchId == matchId)
            .Single();

        if (existing is not null)
        {
            existing.WinnerId = winnerId;
            existing.Score = string.IsNullOrWhiteSpace(score) ? null : score;
            existing.EnteredBy = enteredBy;
            var updated = await existing.Update<LeagueMatchResult>();
            return updated.Model;
        }

        var result = new LeagueMatchResult
        {
            MatchId = matchId,
            WinnerId = winnerId,
            Score = string.IsNullOrWhiteSpace(score) ? null : score,
            EnteredBy = enteredBy
        };

        var insertResult = await _client.From<LeagueMatchResult>().Insert(result);

        // Mark match as completed
        var matchOptions = new QueryOptions { Returning = QueryOptions.ReturnType.Representation };
        await _client.From<LeagueMatch>()
            .Where(x => x.Id == matchId)
            .Set(x => x.Status, "completed")
            .Update(matchOptions);

        return insertResult.Model;
    }
}
