using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RdClean.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{ 
    public DbSet<Image> Images { get; set; }
    
    public DbSet<Redraw> Redraws { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}