using Microsoft.AspNetCore.Identity;
using TransManagement.Domain.Entities;

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

    public Customer? Customer { get; private set; }

    public Driver? Driver { get; private set; }
}
