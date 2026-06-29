using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.BookingRules;

public interface IBookingRuleService
{
    Task<ClubBookingRule?> GetRulesAsync(Guid clubId);

    Task<bool> UpdateRulesAsync(Guid clubId, decimal minHoursPerBooking, decimal maxHoursPerBooking, int maxAdvanceDays);
}
