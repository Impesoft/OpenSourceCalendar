namespace OpenSourceCalendar.Types;

public class RoomState
{
    public DateTime Date { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public RoomStatus State { get; set; }
    public Guid UserId { get; set; } // The user who booked or optioned the room
}
public enum RoomStatus
{
    Available,
    OptionBooked,
    Booked,
    Blocked
}