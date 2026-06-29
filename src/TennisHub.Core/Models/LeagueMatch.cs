using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("league_matches")]
public class LeagueMatch : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("league_id")]
    public Guid LeagueId { get; set; }

    [Column("player1_id")]
    public Guid Player1Id { get; set; }

    [Column("player2_id")]
    public Guid Player2Id { get; set; }

    [Column("scheduled_date")]
    public DateOnly? ScheduledDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
