using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OpenSourceCalendar.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BookingEntity> Bookings { get; set; }
    public DbSet<Room> Rooms { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the ApplicationUser entity if not already configured by ASP.NET Identity




        // configure room entity
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomID);
            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(50);

        });

        // Booking configuration
        modelBuilder.Entity<BookingEntity>(entity =>
        {
            entity.HasKey(e => e.BookingID);
            entity.HasOne(e => e.Room)
                  .WithMany(r => r.Bookings)
                  .HasForeignKey(e => e.RoomID);

            entity.HasOne(e => e.ApplicationUser)
                  .WithMany()
                  .HasForeignKey(e => e.UserID);

            entity.Property(e => e.StartDate)
                  .HasColumnType("date");
            entity.Property(e => e.EndDate)
                  .HasColumnType("date");
            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasColumnType("varchar(50)");
        });
    }
}
