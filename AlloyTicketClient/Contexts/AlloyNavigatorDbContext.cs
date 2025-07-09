using Microsoft.EntityFrameworkCore;

public class AlloyNavigatorDbContext : DbContext
{
    public AlloyNavigatorDbContext(DbContextOptions<AlloyNavigatorDbContext> options)
        : base(options)
    {
    }

    public DbSet<FormFieldDto> FormFieldResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormFieldDto>().HasNoKey();
        modelBuilder.Entity<FormFieldDto>()
            .Property(e => e.FieldType)
            .HasConversion<int?>(); 
        modelBuilder.Entity<FormFieldDto>().Ignore(f => f.Options); 
        base.OnModelCreating(modelBuilder);
    }
}
