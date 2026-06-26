using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("default_club_id")]
    public Guid? DefaultClubId { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("racket")]
    public string? Racket { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
