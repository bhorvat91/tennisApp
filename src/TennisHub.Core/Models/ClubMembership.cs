using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using TennisHub.Core.Enums;

namespace TennisHub.Core.Models;

[Table("club_memberships")]
public class ClubMembership : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("club_id")]
    public Guid ClubId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("role")]
    public string RoleValue { get; set; } = "member";

    [Column("status")]
    public string StatusValue { get; set; } = "pending";

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }

    [JsonIgnore]
    public MembershipRole Role => Enum.TryParse<MembershipRole>(RoleValue, true, out var role)
        ? role
        : MembershipRole.Member;

    [JsonIgnore]
    public MembershipStatus Status => Enum.TryParse<MembershipStatus>(StatusValue, true, out var status)
        ? status
        : MembershipStatus.Pending;
}
