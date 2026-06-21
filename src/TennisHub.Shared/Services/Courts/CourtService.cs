using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Courts;

public class CourtService(Supabase.Client client) : ICourtService
{
    private readonly Supabase.Client _client = client;

    public async Task<IReadOnlyList<Court>> GetCourtsAsync(Guid clubId)
    {
        var response = await _client.From<Court>()
            .Where(x => x.ClubId == clubId)
            .Order(x => x.Name, Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<Court?> CreateCourtAsync(Guid clubId, string name, string? surfaceType, bool hasFloodlights)
    {
        var court = new Court
        {
            ClubId = clubId,
            Name = name.Trim(),
            SurfaceType = NormalizeSurfaceType(surfaceType),
            HasFloodlights = hasFloodlights
        };

        var result = await _client.From<Court>().Insert(court);
        return result.Model is { } createdCourt ? createdCourt : null;
    }

    public async Task<bool> UpdateCourtAsync(Guid courtId, string name, string? surfaceType, bool hasFloodlights)
    {
        var court = await _client.From<Court>()
            .Where(x => x.Id == courtId)
            .Single();

        if (court is null)
        {
            return false;
        }

        court.Name = name.Trim();
        court.SurfaceType = NormalizeSurfaceType(surfaceType);
        court.HasFloodlights = hasFloodlights;

        var result = await court.Update<Court>();

        return result.Models.Count > 0;
    }

    public async Task<bool> DeleteCourtAsync(Guid courtId)
    {
        var court = await _client.From<Court>()
            .Where(x => x.Id == courtId)
            .Single();

        if (court is null)
        {
            return false;
        }

        await court.Delete<Court>();
        return true;
    }

    private static string? NormalizeSurfaceType(string? surfaceType)
    {
        return string.IsNullOrWhiteSpace(surfaceType) ? null : surfaceType.Trim();
    }
}
