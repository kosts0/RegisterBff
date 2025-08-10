using Microsoft.EntityFrameworkCore;
using WorkerService1.DbEntity;

namespace WorkerService1;

public class PostgresDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public PostgresDbContext()
    {
        Database.EnsureCreated();
    }
}