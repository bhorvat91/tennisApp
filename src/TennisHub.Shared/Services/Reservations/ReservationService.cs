using Supabase.Postgrest;
using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Reservations;

public class ReservationService(Supabase.Client client) : IReservationService
{
    private readonly Supabase.Client _client = client;

    public async Task<IReadOnlyList<Reservation>> GetReservationsForCourtAsync(Guid courtId, DateOnly date)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var response = await _client.From<Reservation>()
            .Where(x => x.CourtId == courtId && x.StatusValue == "confirmed")
            .Filter("start_time", Constants.Operator.GreaterThanOrEqual, startOfDay.ToString("o"))
            .Filter("start_time", Constants.Operator.LessThanOrEqual, endOfDay.ToString("o"))
            .Order(x => x.StartTime, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IReadOnlyList<Reservation>> GetReservationsForClubAsync(Guid clubId, DateOnly date)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var response = await _client.From<Reservation>()
            .Where(x => x.ClubId == clubId && x.StatusValue == "confirmed")
            .Filter("start_time", Constants.Operator.GreaterThanOrEqual, startOfDay.ToString("o"))
            .Filter("start_time", Constants.Operator.LessThanOrEqual, endOfDay.ToString("o"))
            .Order(x => x.StartTime, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IReadOnlyList<Reservation>> GetMyReservationsForCourtAsync(Guid courtId, Guid userId, DateOnly date)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var response = await _client.From<Reservation>()
            .Where(x => x.CourtId == courtId && x.UserId == userId && x.StatusValue == "confirmed")
            .Filter("start_time", Constants.Operator.GreaterThanOrEqual, startOfDay.ToString("o"))
            .Filter("start_time", Constants.Operator.LessThanOrEqual, endOfDay.ToString("o"))
            .Order(x => x.StartTime, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<Reservation?> CreateReservationAsync(Guid courtId, Guid clubId, Guid userId, DateTime startTime, DateTime endTime)
    {
        var reservation = new Reservation
        {
            CourtId = courtId,
            ClubId = clubId,
            UserId = userId,
            StartTime = startTime,
            EndTime = endTime,
            StatusValue = "confirmed"
        };

        var result = await _client.From<Reservation>().Insert(reservation);
        return result.Model;
    }

    public async Task<bool> CancelReservationAsync(Guid reservationId)
    {
        var reservation = await _client.From<Reservation>()
            .Where(x => x.Id == reservationId)
            .Single();

        if (reservation is null)
        {
            return false;
        }

        var options = new QueryOptions
        {
            Returning = QueryOptions.ReturnType.Representation
        };

        var result = await _client.From<Reservation>()
            .Where(x => x.Id == reservationId)
            .Set(x => x.StatusValue, "cancelled")
            .Update(options);

        return result.Models.Count > 0;
    }
}
