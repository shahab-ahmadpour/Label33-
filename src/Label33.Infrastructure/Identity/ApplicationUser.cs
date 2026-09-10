using Microsoft.AspNetCore.Identity;

namespace Label33.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
}
