using DenoLite.Domain.Common;

namespace DenoLite.Domain.Entities
{
    public class ProjectInvitation : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid InvitedByUserId { get; set; }   // owner who sent it
        public string Email { get; set; } = string.Empty;

        // random, unguessable token for link
        public string Token { get; set; } = string.Empty;

        // "Pending", "Accepted", "Expired", "Cancelled"
        public string Status { get; set; } = "Pending";

        public DateTime ExpiresAt { get; set; }

        public DateTime? AcceptedAt { get; set; }
    }
}