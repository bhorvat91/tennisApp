using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("courts")]
public class Court : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("club_id")]
    public Guid ClubId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("surface_type")]
    public string? SurfaceType { get; set; }

    [Column("has_floodlights")]
    public bool HasFloodlights { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
