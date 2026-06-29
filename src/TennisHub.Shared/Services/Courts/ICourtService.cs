using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Courts;

public interface ICourtService
{
    Task<IReadOnlyList<Court>> GetCourtsAsync(Guid clubId);

    Task<Court?> CreateCourtAsync(Guid clubId, string name, string? surfaceType, bool hasFloodlights);

    Task<bool> UpdateCourtAsync(Guid courtId, string name, string? surfaceType, bool hasFloodlights);

    Task<bool> DeleteCourtAsync(Guid courtId);
}
