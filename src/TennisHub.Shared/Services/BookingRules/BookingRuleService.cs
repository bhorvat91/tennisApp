using TennisHub.Core.Models;

namespace TennisHub.Shared.Services.BookingRules;

public class BookingRuleService(Supabase.Client client) : IBookingRuleService
{
    private readonly Supabase.Client _client = client;

    public async Task<ClubBookingRule?> GetRulesAsync(Guid clubId)
    {
        return await _client.From<ClubBookingRule>()
            .Where(x => x.ClubId == clubId)
            .Single();
    }

    public async Task<bool> UpdateRulesAsync(Guid clubId, decimal minHoursPerBooking, decimal maxHoursPerBooking, int maxAdvanceDays)
    {
        var rule = await _client.From<ClubBookingRule>()
            .Where(x => x.ClubId == clubId)
            .Single();

        if (rule is null)
        {
            var createResult = await _client.From<ClubBookingRule>().Insert(new ClubBookingRule
            {
                ClubId = clubId,
                MinHoursPerBooking = minHoursPerBooking,
                MaxHoursPerBooking = maxHoursPerBooking,
                MaxAdvanceDays = maxAdvanceDays
            });

            return createResult.Model is not null;
        }

        rule.MinHoursPerBooking = minHoursPerBooking;
        rule.MaxHoursPerBooking = maxHoursPerBooking;
        rule.MaxAdvanceDays = maxAdvanceDays;

        var updateResult = await rule.Update<ClubBookingRule>();
        return updateResult.Models.Count > 0;
    }
}
