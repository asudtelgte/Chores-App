using Microsoft.EntityFrameworkCore;
using ChoreTracker.API.Models;
using System.Text.Json;

namespace ChoreTracker.API.Data;

public class ChoreTrackerDbContext : DbContext
{
    public ChoreTrackerDbContext(DbContextOptions<ChoreTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Chore> Chores { get; set; }
    public DbSet<ChoreCompletion> ChoreCompletions { get; set; }
    public DbSet<ChoreSkip> ChoreSkips { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.CreatedDate).IsRequired();

            // Add unique constraint for email
            entity.HasIndex(u => u.Email).IsUnique();

            // Configure one-to-many relationship with Chores
            entity.HasMany(u => u.Chores)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship with ChoreCompletions
            entity.HasMany(u => u.Completions)
                .WithOne(cc => cc.User)
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship with ChoreSkips
            entity.HasMany(u => u.Skips)
                .WithOne(cs => cs.User)
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-one relationship with UserPreferences
            entity.HasOne(u => u.Preferences)
                .WithOne(up => up.User)
                .HasForeignKey<UserPreferences>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Chore entity
        modelBuilder.Entity<Chore>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Description).HasMaxLength(1000);
            entity.Property(c => c.Category).IsRequired();
            entity.Property(c => c.CreatedDate).IsRequired();
            entity.Property(c => c.IsActive).IsRequired();
            entity.Property(c => c.UserId).IsRequired();

            // Store RecurrencePattern as JSON
            entity.Property(c => c.RecurrencePattern)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<RecurrencePattern>(v, (JsonSerializerOptions?)null))
                .HasColumnType("jsonb");

            // Configure relationships
            entity.HasMany(c => c.Completions)
                .WithOne(cc => cc.Chore)
                .HasForeignKey(cc => cc.ChoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Skips)
                .WithOne(cs => cs.Chore)
                .HasForeignKey(cs => cs.ChoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ChoreCompletion entity
        modelBuilder.Entity<ChoreCompletion>(entity =>
        {
            entity.HasKey(cc => cc.Id);
            entity.Property(cc => cc.ChoreId).IsRequired();
            entity.Property(cc => cc.CompletionDate).IsRequired();
            entity.Property(cc => cc.NecessityRating).IsRequired();
            entity.Property(cc => cc.Notes).HasMaxLength(500);
            entity.Property(cc => cc.UserId).IsRequired();
        });

        // Configure UserPreferences entity
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(up => up.Id);
            entity.Property(up => up.DayStartHour).IsRequired();
            entity.Property(up => up.CreatedDate).IsRequired();
            entity.Property(up => up.ModifiedDate).IsRequired();
            entity.Property(up => up.UserId).IsRequired();
        });

        // Configure ChoreSkip entity
        modelBuilder.Entity<ChoreSkip>(entity =>
        {
            entity.HasKey(cs => cs.Id);
            entity.Property(cs => cs.ChoreId).IsRequired();
            entity.Property(cs => cs.UserId).IsRequired();
            entity.Property(cs => cs.SkippedDeadline).IsRequired();
            entity.Property(cs => cs.RecordedDate).IsRequired();
            entity.Property(cs => cs.Reason).HasMaxLength(500);
        });
    }
}
