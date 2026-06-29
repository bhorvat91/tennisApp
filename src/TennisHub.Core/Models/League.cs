using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("leagues")]
public class League : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("club_id")]
    public Guid ClubId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("season")]
    public string? Season { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("points_per_win")]
    public int PointsPerWin { get; set; } = 2;

    [Column("points_per_loss")]
    public int PointsPerLoss { get; set; } = 1;

    [Column("third_set_format")]
    public string ThirdSetFormat { get; set; } = "normal";

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
