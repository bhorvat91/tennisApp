using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("league_players")]
public class LeaguePlayer : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("league_id")]
    public Guid LeagueId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }
}
