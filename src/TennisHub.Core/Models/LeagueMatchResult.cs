using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("league_match_results")]
public class LeagueMatchResult : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("match_id")]
    public Guid MatchId { get; set; }

    [Column("winner_id")]
    public Guid WinnerId { get; set; }

    [Column("score")]
    public string? Score { get; set; }

    [Column("entered_by")]
    public Guid EnteredBy { get; set; }

    [Column("entered_at")]
    public DateTime EnteredAt { get; set; }
}
