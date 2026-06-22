using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("club_booking_rules")]
public class ClubBookingRule : BaseModel
{
    [PrimaryKey("club_id", false)]
    [Column("club_id")]
    public Guid ClubId { get; set; }

    [Column("max_hours_per_booking")]
    public decimal MaxHoursPerBooking { get; set; }

    [Column("max_advance_days")]
    public int MaxAdvanceDays { get; set; }
}
