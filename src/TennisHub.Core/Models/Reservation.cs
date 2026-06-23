using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;
using TennisHub.Core.Enums;

namespace TennisHub.Core.Models;

[Table("reservations")]
public class Reservation : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("court_id")]
    public Guid CourtId { get; set; }

    [Column("club_id")]
    public Guid ClubId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("start_time")]
    public DateTime StartTime { get; set; }

    [Column("end_time")]
    public DateTime EndTime { get; set; }

    [Column("status")]
    public string StatusValue { get; set; } = "confirmed";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public ReservationStatus Status => Enum.TryParse<ReservationStatus>(StatusValue, true, out var s)
        ? s
        : ReservationStatus.Confirmed;
}
