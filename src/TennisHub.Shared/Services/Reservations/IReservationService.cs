using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.Reservations;

public interface IReservationService
{
    Task<IReadOnlyList<Reservation>> GetReservationsForCourtAsync(Guid courtId, DateOnly date);

    Task<IReadOnlyList<Reservation>> GetMyReservationsForCourtAsync(Guid courtId, Guid userId, DateOnly date);

    Task<Reservation?> CreateReservationAsync(Guid courtId, Guid clubId, Guid userId, DateTime startTime, DateTime endTime);

    Task<bool> CancelReservationAsync(Guid reservationId);
}
