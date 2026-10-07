using Microsoft.AspNetCore.Identity;

namespace TransManagement.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString();
    }

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];
}

