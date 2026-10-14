using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core Code-First DbContext for RaceDay. The Fluent API configuration
/// below deliberately mirrors docs/RaceDay_Database.sql table by table,
/// column by column, so the Part 2 schema matches the Part 1 ERD and SQL
/// script exactly.
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Organiser> Organisers { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOrganisers(modelBuilder);
        ConfigureParticipants(modelBuilder);
        ConfigureEvents(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigureEnrolments(modelBuilder);
        ConfigureResults(modelBuilder);
    }

    private static void ConfigureOrganisers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organiser>(entity =>
        {
            entity.ToTable("Organisers");
            entity.HasKey(e => e.OrganiserID);

            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.OrganisationName).HasMaxLength(150);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    private static void ConfigureParticipants(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.ToTable("Participants");
            entity.HasKey(e => e.ParticipantID);

            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.DateOfBirth).IsRequired();
            entity.Property(e => e.EmergencyContactName).HasMaxLength(100);
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    private static void ConfigureEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.EventID);

            entity.Property(e => e.EventName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EventDate).IsRequired();
            entity.Property(e => e.Location).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Distance).HasColumnType("decimal(6,2)");
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            // Every Event belongs to exactly one Organiser (Organiser 1 : M Events).
            entity.HasOne(e => e.Organiser)
                  .WithMany(o => o.Events)
                  .HasForeignKey(e => e.OrganiserID)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.CategoryID);

            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MinAge).HasDefaultValue(0);
            entity.Property(e => e.MaxAge).HasDefaultValue(99);
            entity.Property(e => e.DistanceKM).HasColumnType("decimal(6,2)");

            // Every Category belongs to exactly one Event (Event 1 : M Categories).
            // Deleting an Event cascades to its Categories, matching the Part 1 SQL script.
            entity.HasOne(e => e.Event)
                  .WithMany(ev => ev.Categories)
                  .HasForeignKey(e => e.EventID)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEnrolments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(e => e.EnrolmentID);

            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("SYSUTCDATETIME()");

            // A Participant may only enrol once per Event - matches
            // UQ_Enrolments_ParticipantEvent in the Part 1 SQL script.
            entity.HasIndex(e => new { e.ParticipantID, e.EventID }).IsUnique();

            // ENROLMENTS is the associative entity resolving the many-to-many
            // relationship between Participants and Events, while also
            // recording which Category was selected. All three foreign keys
            // use Restrict so deleting a Participant/Event/Category never
            // silently cascades away enrolment history.
            entity.HasOne(e => e.Participant)
                  .WithMany(p => p.Enrolments)
                  .HasForeignKey(e => e.ParticipantID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Event)
                  .WithMany(ev => ev.Enrolments)
                  .HasForeignKey(e => e.EventID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(e => e.CategoryID)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureResults(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(e => e.ResultID);

            entity.Property(e => e.RecordedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            // At most one Result per Enrolment - the 1 : 0..1 cardinality
            // from the ERD, matching UQ_Results_Enrolment in the SQL script.
            entity.HasIndex(e => e.EnrolmentID).IsUnique();

            entity.HasOne(e => e.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(e => e.EnrolmentID)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RecordedByOrganiser)
                  .WithMany()
                  .HasForeignKey(e => e.RecordedByOrganiserID)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
