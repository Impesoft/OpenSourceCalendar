using OpenSourceCalendar.Data;
using OpenSourceCalendar.Types;

namespace CasaAdelia.Components.Types;
public class OverviewBooking
{
    public RoomState RoomState { get; set; }
    public RoomStatus NewStatus { get; set; }
    public BookingEntity BookingEntity { get; set; }
    public bool IsModified { get; set; }
    public BookingInfo BookingInfo { get; set; }
}
