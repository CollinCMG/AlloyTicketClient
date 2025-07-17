using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AlloyTicketClient.Models;
using System.Text.Json;

namespace AlloyTicketClient.Contexts
{
    public class AlloyTicketRulesDbContext : DbContext
    {
        public AlloyTicketRulesDbContext(DbContextOptions<AlloyTicketRulesDbContext> options)
            : base(options)
        {
        }

        public DbSet<RuleConfig> AlloyTicketRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var targetListConverter = new ValueConverter<List<TargetFieldInfo>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => string.IsNullOrEmpty(v) ? new List<TargetFieldInfo>() : JsonSerializer.Deserialize<List<TargetFieldInfo>>(v, (JsonSerializerOptions)null)
            );

            modelBuilder.Entity<RuleConfig>(entity =>
            {
                entity.HasKey(e => e.RuleId);
                entity.Property(e => e.FormId).IsRequired();

                entity.Property(e => e.TriggerField).IsRequired();
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.FormName);
                entity.Property(e => e.TriggerFieldLabel);
                entity.Property(e => e.TriggerValue); // Add mapping for TriggerValue
                entity.Property(e => e.IsSet);
                entity.Property(e => e.TargetFieldLabels);
                entity.Property(e => e.TargetList)
                      .HasConversion(targetListConverter)
                      .HasColumnType("nvarchar(max)");
            });
        }
    }
}