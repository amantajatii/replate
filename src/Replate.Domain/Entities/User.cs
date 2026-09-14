using Replate.Domain.Enums;

namespace Replate.Domain.Entities;

public abstract class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public abstract UserRole Role { get; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual void Login(string email, string password)
    {
    }

    public virtual void Logout()
    {
    }
}
