using CasaAdelia.Components.Types;
using CasaAdelia.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenSourceCalendar.Data;
using OpenSourceCalendar.Types;
using System.Globalization;

public partial class BookingService(IDbContextFactory<ApplicationDbContext> dbContextFactory, IHubContext<SignalRService> hubContext) : IBookingService
{
    public async Task<IEnumerable<RoomState>> LoadRoomStatesForMonth(DateTime month)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        await CleanUpOptionBooked();
        DateTime startOfMonth = new(month.Year, month.Month, 1);
        DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var roomNames = await _dbContext.Rooms.Select(r => r.Name).ToListAsync();
        // set all roomStates to available
        var roomStates = roomNames.SelectMany(roomName =>
            Enumerable.Range(0, (endOfMonth - startOfMonth).Days + 1)
                .Select(offset => startOfMonth.AddDays(offset))
                .Select(date => new RoomState
                {
                    Date = date,
                    RoomName = roomName,
                    State = RoomStatus.Available
                })
        ).ToList();
        //get bookings
        var bookings = await _dbContext.Bookings
            .Include(b => b.Room)
            .Where(b => b.StartDate <= endOfMonth && b.EndDate >= startOfMonth)
            .ToListAsync();
        //set roomStates to appropriate states
        foreach (var roomName in roomNames)
        {
            foreach (var booking in bookings.Where(b => b.Room.Name == roomName))
            {
                for (var date = booking.StartDate; date <= booking.EndDate; date = date.AddDays(1))
                {
                    if (date >= startOfMonth && date <= endOfMonth)
                    {
                        if (!roomStates.Any(rs => rs.Date == date && rs.RoomName == roomName))
                        {
                            roomStates.Add(new RoomState
                            {
                                Date = date,
                                RoomName = roomName,
                                State = Enum.Parse<RoomStatus>(booking.Status),
                                UserId = Guid.Parse(booking.UserID)
                            });
                        }
                        else
                        {
                            roomStates.Where(rs => rs.Date == date && rs.RoomName == roomName).First().State = Enum.Parse<RoomStatus>(booking.Status);
                            roomStates.Where(rs => rs.Date == date && rs.RoomName == roomName).First().UserId = Guid.Parse(booking.UserID);
                        }

                    }
                }
            }
        }

        return roomStates;
    }

    private async Task CleanUpOptionBooked()
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var bookings = _dbContext.Bookings;
        var toRemove = bookings.Where(booking =>
            booking.Status == RoomStatus.OptionBooked.ToString() &&
            booking.CreatedAt.AddHours(1) < DateTime.UtcNow
        ).ToList();

        _dbContext.Bookings.RemoveRange(toRemove);
        var deletedItemsCount = await _dbContext.SaveChangesAsync();
        if (deletedItemsCount > 0)
        {
            var message = "Deleted old booking options (older than an hour)";
            await hubContext.Clients.All.SendAsync("ReceiveMessage", message);
        }
    }

    public async Task UpdateRoomState(DateTime day, string room, Guid UserID)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var booking = await _dbContext.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Room.Name == room && b.Status == RoomStatus.OptionBooked.ToString() && b.UserID == UserID.ToString());

        bool isConflict = await BookingService.isConflict(day, room, UserID, _dbContext);
        if (isConflict)
        {
            // If there's a conflict, do not update the booking
            return;
        }
        if (booking != null)
        {

            if (day == booking.StartDate || day == booking.EndDate)
            {
                _dbContext.Bookings.Remove(booking);
            }
            else if (day < booking.StartDate)
            {
                booking.StartDate = day;
                _dbContext.Bookings.Update(booking);
            }
            else if (day > booking.EndDate)
            {
                booking.EndDate = day;
                _dbContext.Bookings.Update(booking);
            }
            else
            {
                if (Math.Abs((day - booking.StartDate).Days) < Math.Abs((day - booking.EndDate).Days))
                {
                    booking.StartDate = day;
                }
                else
                {
                    booking.EndDate = day;
                }
                _dbContext.Bookings.Update(booking);
            }
        }
        else
        {
            var dbRoom = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.Name == room);
            if (dbRoom == null)
            {
                return;
            }

            booking = new BookingEntity
            {
                RoomID = dbRoom.RoomID,
                Room = dbRoom,
                UserID = UserID.ToString(),
                StartDate = day,
                EndDate = day,
                Status = RoomStatus.OptionBooked.ToString()
            };
            _dbContext.Bookings.Add(booking);
        }

        await _dbContext.SaveChangesAsync();
        await hubContext.Clients.All.SendAsync("ReceiveMessage", "Booking updated");
    }

    private static async Task<bool> isConflict(DateTime day, string room, Guid UserID, ApplicationDbContext _dbContext)
    {
        // Fetch all relevant bookings for the room
        var relevantBookings = await _dbContext.Bookings
            .Where(b =>
                b.Room.Name == room &&
                (
                    b.Status == RoomStatus.Booked.ToString() || // All booked periods
                    b.Status == RoomStatus.Blocked.ToString() || // All blocked periods
                    (b.Status == RoomStatus.OptionBooked.ToString() && b.UserID != UserID.ToString()) // Other users' option-booked periods
                )
            )
            .ToListAsync();

        // Fetch the user's current option-booked period, if any
        var userBooking = await _dbContext.Bookings
            .FirstOrDefaultAsync(b =>
                b.Room.Name == room &&
                b.Status == RoomStatus.OptionBooked.ToString() &&
                b.UserID == UserID.ToString()
            );

        DateTime proposedStartDate;
        DateTime proposedEndDate;

        if (userBooking != null)
        {
            // Adjust start or end date based on user interaction
            if (day < userBooking.StartDate)
            {
                proposedStartDate = day;
                proposedEndDate = userBooking.EndDate;
            }
            else if (day > userBooking.EndDate)
            {
                proposedStartDate = userBooking.StartDate;
                proposedEndDate = day;
            }
            else
            {
                return false; // If within the user's own period, no conflict
            }
        }
        else
        {
            // No existing option-booked period; treat the day as both start and end date
            proposedStartDate = day;
            proposedEndDate = day;
        }

        // Check for any conflicts with booked, blocked, or other users' option-booked periods
        foreach (var booking in relevantBookings)
        {
            if (
                (proposedStartDate <= booking.StartDate && proposedEndDate >= booking.EndDate) || // Wraparound conflict
                (proposedStartDate >= booking.StartDate && proposedStartDate <= booking.EndDate) || // Overlap at start
                (proposedEndDate >= booking.StartDate && proposedEndDate <= booking.EndDate) // Overlap at end
            )
            {
                return true; // Conflict detected
            }
        }

        return false; // No conflicts detected
    }

    public async Task<Dictionary<Guid, string>> GetRoomNames()
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var rooms = await _dbContext.Rooms.ToDictionaryAsync(r => r.RoomID, r => r.Name);
        return rooms.OrderBy(r => r.Value).ToDictionary(r => r.Key, r => r.Value);
    }

    public async Task<IEnumerable<RoomState>> GetBookingsForUser(string userId)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();

        var bookings = await _dbContext.Bookings
            .Include(b => b.Room)
            .Where(b => b.UserID == userId)
            .ToListAsync();

        var roomStates = new List<RoomState>();

        foreach (var booking in bookings)
        {
            for (var date = booking.StartDate; date <= booking.EndDate; date = date.AddDays(1))
            {
                roomStates.Add(new RoomState
                {
                    Date = date,
                    RoomId = booking.RoomID,
                    RoomName = booking.Room.Name,
                    State = Enum.Parse<RoomStatus>(booking.Status),
                    UserId = Guid.Parse(booking.UserID)
                });
            }
        }

        return roomStates;
    }
    public async Task UpdateUserInfo(BookingInfo bookingInfo)
    {
        var userID = bookingInfo.UserId.ToString();
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userID);

        if (user != null)
        {
            user.UserName = bookingInfo.UserName;
            user.Email = bookingInfo.RecipientEmail;
            user.SubscribedToNewsletter = bookingInfo.NewsLetter;
            user.VerificationCode = bookingInfo.VerificationCode;
            _dbContext.Users.Update(user);
        }
        _dbContext.SaveChanges();
    }
    public async Task ConfirmBooking(Guid userId)
    {
        BookingEntity? booking = new();
        await using var _dbContext = dbContextFactory.CreateDbContext();
        booking = await _dbContext.Bookings
            .Include(b => b.ApplicationUser)
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Status == RoomStatus.OptionBooked.ToString() && b.UserID == userId.ToString());
        if (booking == null)
        {
            return;
        }
        booking.Status = RoomStatus.Booked.ToString();
        _dbContext.Bookings.Update(booking);
        _dbContext.SaveChanges();
    }

    public async Task<string> GetBookingConfirmedText(Guid userId)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var booking = await _dbContext.Bookings
                    .Include(b => b.ApplicationUser)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Status == RoomStatus.OptionBooked.ToString() && b.UserID == userId.ToString());
        if (booking == null) throw new ArgumentNullException("booking");
        var text = $"Uw boeking van {booking.StartDate.ToString("dddd dd MMMM yyyy", CultureInfo.CreateSpecificCulture("nl-BE"))} tot {booking.EndDate.ToString("dddd dd MMMM yyyy", CultureInfo.CreateSpecificCulture("nl-BE"))} voor {booking.Room.Name} is bevestigd";
        text += Environment.NewLine + Environment.NewLine;
        text += $"Sua reserva de {booking.StartDate.ToString("dddd, dd 'de' MMMM 'de' yyyy", CultureInfo.CreateSpecificCulture("pt-PT"))} até {booking.EndDate.ToString("dddd, dd 'de' MMMM 'de' yyyy", CultureInfo.CreateSpecificCulture("pt-PT"))} para {booking.Room.Name} foi confirmada";
        text += Environment.NewLine + Environment.NewLine;
        text += $"Your booking from {booking.StartDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture)} till {booking.EndDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture)} for {booking.Room.Name} has been confirmed";
        text += Environment.NewLine + Environment.NewLine;
        text += "Welcome to Casa Adelia";
        return text;
    }

    public async Task<BookingInfo?> GetUserInfo(string verificationCode, string userId)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var booking = await _dbContext.Bookings
    .Include(b => b.ApplicationUser)
    .Include(b => b.Room)
    .FirstOrDefaultAsync(b => b.Status == RoomStatus.OptionBooked.ToString() && b.ApplicationUser.VerificationCode == verificationCode);
        if (booking == null)
        {
            return await GetUserInfoById(userId);
        }

        return new BookingInfo
        {
            RecipientEmail = booking.ApplicationUser.Email,
            UserName = booking.ApplicationUser.UserName,
            UserId = Guid.Parse(booking.UserID),
            NewsLetter = booking.ApplicationUser.SubscribedToNewsletter,
            VerificationCode = booking.ApplicationUser.VerificationCode
        };

    }
    private async Task<BookingInfo> GetUserInfoById(string id)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var booking = await _dbContext.Bookings
    .Include(b => b.ApplicationUser)
    .Include(b => b.Room)
    .FirstOrDefaultAsync(b => b.Status == RoomStatus.OptionBooked.ToString() && b.ApplicationUser.Id == id);

        if (booking == null) throw new ArgumentNullException(nameof(booking));
        return new BookingInfo
        {
            RecipientEmail = booking.ApplicationUser.Email,
            UserName = booking.ApplicationUser.UserName,
            UserId = Guid.Parse(booking.UserID),
            NewsLetter = booking.ApplicationUser.SubscribedToNewsletter,
            VerificationCode = booking.ApplicationUser.VerificationCode
        };
    }

    public async Task<string> GetBookingConfirmedHtml(Guid userId)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();
        var booking = await _dbContext.Bookings
                    .Include(b => b.ApplicationUser)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Status == RoomStatus.OptionBooked.ToString() && b.UserID == userId.ToString());

        if (booking == null) throw new ArgumentNullException(nameof(booking));

        var nlCulture = CultureInfo.CreateSpecificCulture("nl-BE");
        var ptCulture = CultureInfo.CreateSpecificCulture("pt-PT");

        var html = $@"
    <html>
        <body>
            <p>Uw boeking van {booking.StartDate.ToString("dddd dd MMMM yyyy", nlCulture)} tot {booking.EndDate.ToString("dddd dd MMMM yyyy", nlCulture)} voor <strong>{booking.Room.Name}</strong> is bevestigd.</p>
            <br>
            <p>Sua reserva de {booking.StartDate.ToString("dddd, dd 'de' MMMM 'de' yyyy", ptCulture)} até {booking.EndDate.ToString("dddd, dd 'de' MMMM 'de' yyyy", ptCulture)} para <strong>{booking.Room.Name}</strong> foi confirmada.</p>
            <br>
            <p>Your booking from {booking.StartDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture)} till {booking.EndDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture)} for <strong>{booking.Room.Name}</strong> has been confirmed.</p>
            <br>
            <p>Welcome to Casa Adelia!</p>
        </body>
    </html>";

        return html;
    }

    public async Task<List<OverviewBooking>> GetAllBookingsAsync()
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();

        var bookings = await _dbContext.Bookings
                             .Include(b => b.ApplicationUser)
                             .Include(b => b.Room)
                             .ToListAsync();
        return bookings.Select(b => new OverviewBooking
        {
            RoomState = new RoomState
            {
                Date = b.StartDate,
                RoomName = b.Room.Name,
                State = Enum.Parse<RoomStatus>(b.Status),
                UserId = Guid.Parse(b.UserID)
            },
            BookingEntity = b,
            BookingInfo = new BookingInfo
            {
                RecipientEmail = b.ApplicationUser.Email,
                UserName = b.ApplicationUser.UserName,
                UserId = Guid.Parse(b.UserID),
                NewsLetter = b.ApplicationUser.SubscribedToNewsletter,
                VerificationCode = b.ApplicationUser.VerificationCode
            }
        }).ToList();
    }

    public async Task DeleteBookingAsync(Guid bookingId)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();

        var booking = await _dbContext.Bookings.FindAsync(bookingId);
        if (booking != null)
        {
            _dbContext.Bookings.Remove(booking);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task ChangeBookingStatus(Guid bookingId, string newStatus)
    {
        await using var _dbContext = dbContextFactory.CreateDbContext();

        var booking = await _dbContext.Bookings.FindAsync(bookingId);
        if (booking != null)
        {
            booking.Status = newStatus;
            await _dbContext.SaveChangesAsync();
        }
    }
}
