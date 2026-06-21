using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace TennisHub.Core.Models;

[Table("clubs")]
public class Club : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
