using Microsoft.EntityFrameworkCore;

public class AlloyNavigatorDbContext : DbContext
{
    public AlloyNavigatorDbContext(DbContextOptions<AlloyNavigatorDbContext> options)
        : base(options)
    {
    }

    // Keyless DbSet for FormFieldDto
    public DbSet<FormFieldDto> FormFieldResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormFieldDto>().HasNoKey();
        base.OnModelCreating(modelBuilder);
    }

    // Define DbSet properties for your tables, for example:
    // public DbSet<YourEntity> YourEntities { get; set; }
}
