using CasaAdelia.Components.Types;
using OpenSourceCalendar.Types;

public interface IBookingService
{
    Task ChangeBookingStatus(Guid bookingId, string newStatus);
    Task ConfirmBooking(Guid userId);
    Task DeleteBookingAsync(Guid bookingId);
    Task<List<OverviewBooking>> GetAllBookingsAsync();
    Task<string> GetBookingConfirmedHtml(Guid userId);
    Task<string> GetBookingConfirmedText(Guid userId);
    Task<IEnumerable<RoomState>> GetBookingsForUser(string userId);
    Task<Dictionary<Guid, string>> GetRoomNames();
    Task<BookingInfo?> GetUserInfo(string verificationCode, string userId);
    Task<IEnumerable<RoomState>> LoadRoomStatesForMonth(DateTime month);
    Task UpdateRoomState(DateTime day, string room, Guid UserID);
    Task UpdateUserInfo(BookingInfo bookingInfo);
}