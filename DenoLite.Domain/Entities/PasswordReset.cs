using DenoLite.Domain.Common;

namespace DenoLite.Domain.Entities
{
    public class PasswordReset : BaseEntity
    {
        public Guid UserId { get; set; }
        public string CodeHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; } = 0;
        public bool IsUsed { get; set; } = false;
    }
}
