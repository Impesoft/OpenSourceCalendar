namespace OpenSourceCalendar.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BookingEntity
{
    [Key]
    public Guid BookingID { get; set; } = Guid.NewGuid();

    public Guid RoomID { get; set; }
    public virtual Room Room { get; set; }

    public string UserID { get; set; } // Changed to string to match ApplicationUser.Id
    public virtual ApplicationUser ApplicationUser { get; set; }

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Status { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


public class Room
{
    [Key]
    public Guid RoomID { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    public virtual ICollection<BookingEntity> Bookings { get; set; } = new HashSet<BookingEntity>();
}
