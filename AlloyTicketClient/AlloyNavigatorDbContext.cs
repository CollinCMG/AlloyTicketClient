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
        modelBuilder.Entity<FormFieldDto>()
            .Property(e => e.FieldType)
            .HasConversion<int?>(); // Map nullable enum to int
        modelBuilder.Entity<FormFieldDto>().Ignore(f => f.Options); // Ignore Options property
        base.OnModelCreating(modelBuilder);
    }

    // Define DbSet properties for your tables, for example:
    // public DbSet<YourEntity> YourEntities { get; set; }
}
