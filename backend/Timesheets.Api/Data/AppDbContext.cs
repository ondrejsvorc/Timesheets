using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Data.Models;

namespace Timesheets.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    // ...

    public AppDbContext()
    {
        throw new NotImplementedException();
    }
}
