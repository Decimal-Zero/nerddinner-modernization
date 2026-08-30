using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NerdDinner.Models
{
    // Ported from the legacy app's Models/IdentityModels.cs (M10,
    // decision-log.md DL-029). Same shape (ApplicationUser : IdentityUser),
    // now ASP.NET Core Identity instead of ASP.NET Identity 2.x/OWIN.
    // Points at the SAME physical "NerdDinner.Identity" LocalDB database
    // the legacy app's ApplicationDbContext already uses -- strangler-fig
    // reuse, same principle as M9's NerdDinnerCoreContext.
    public class ApplicationUser : IdentityUser
    {
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
