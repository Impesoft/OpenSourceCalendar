using System.Globalization;
using CasaAdelia.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using OpenSourceCalendar.Types;

namespace OpenSourceCalendar.Components.Pages;

public partial class Booking
{
    [Inject] private IConfiguration? Configuration { get; set; }
    [Inject] IBookingService? BookingService { get; set; }
    [Inject] NavigationManager? NavigationManager { get; set; }
    [Inject] BookingStateService? _bookingStateService { get; set; }
    [Inject] VisitorService _visitorService { get; set; }
    [Inject] IHttpContextAccessor _httpContextAccessor { get; set; }

    private HubConnection? _hubConnection;
    private bool _busy = false;
    private bool _loading = true;
    public Guid UserID { get; set; } = Guid.Empty;
    private Dictionary<Guid, string> roomNames = new();
    public List<DateTime> DaysInSelectedMonth { get; set; } = new();
    private DateTime selectedMonth = DateTime.Now;
    [Parameter]
    public DateTime SelectedMonth
    {
        get => selectedMonth;
        set
        {
            selectedMonth = value;
            OnMonthChanged();
        }
    }
    private IEnumerable<RoomState> RoomStates = new List<RoomState>();
    private IEnumerable<RoomState> AllBookingsForUser = new List<RoomState>();
    private string SelectedMonthName => SelectedMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
    private string PreviousMonthName => SelectedMonth.AddMonths(-1).ToString("MMMM", CultureInfo.InvariantCulture);
    private string NextMonthName => SelectedMonth.AddMonths(1).ToString("MMMM", CultureInfo.InvariantCulture);

    public double Korting;
    public double SeasonPrice { get; set; }
    public double OffSeasonPrice { get; set; }
    public bool Disabled;

    protected override void OnInitialized()
    {
        _loading = true;
        var uri = new Uri(NavigationManager.Uri);
        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        ArgumentNullException.ThrowIfNull(Configuration, nameof(Configuration));

        // Set prices
        SeasonPrice = Configuration.GetValue<double>("Pricing:SeasonPrice");
        OffSeasonPrice = Configuration.GetValue<double>("Pricing:OffSeasonPrice");

        // Set season prices in BookingStateService
        _bookingStateService.SeasonPrice = SeasonPrice;
        _bookingStateService.OffSeasonPrice = OffSeasonPrice;

        // Get visitor ID
        UserID = _visitorService.GetVisitorId(_httpContextAccessor.HttpContext);

        // Handle month parameter from URL
        if (queryParams.TryGetValue("Month", out var monthString) && int.TryParse(monthString, out var month))
        {
            if (month >= 1 && month <= 12)
            {
                var currentYear = DateTime.Now.Year;
                SelectedMonth = month < DateTime.Now.Month
                    ? new DateTime(currentYear + 1, month, 1)
                    : new DateTime(currentYear, month, 1);
            }
        }

        base.OnInitialized();
    }

    private async Task OnMonthChanged()
    {
        await GenerateDaysForMonth(SelectedMonth);
        await UpdateUI();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ArgumentNullException.ThrowIfNull(BookingService, nameof(BookingService));
            ArgumentNullException.ThrowIfNull(_bookingStateService, nameof(_bookingStateService));
            ArgumentNullException.ThrowIfNull(Configuration, nameof(Configuration));
            ArgumentNullException.ThrowIfNull(NavigationManager, nameof(NavigationManager));

            roomNames = await BookingService.GetRoomNames();
            
            await GenerateDaysForMonth(SelectedMonth);
            AllBookingsForUser = await BookingService.GetBookingsForUser(UserID.ToString());

            _hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(NavigationManager.ToAbsoluteUri("/notificationhub"))
                .Build();

            _hubConnection.On<string>("ReceiveMessage", async (message) => await UpdateUI());

            _loading = false;
            await UpdateUI();
            await InvokeAsync(StateHasChanged);
            await _hubConnection.StartAsync();
        }
    }

    private async Task UpdateUI()
    {
        if (_loading) return;
        _busy = true;

        RoomStates = await BookingService.LoadRoomStatesForMonth(SelectedMonth);
        AllBookingsForUser = await BookingService.GetBookingsForUser(UserID.ToString());

        _busy = false;
        await InvokeAsync(StateHasChanged);
    }

    private string GetStartDateAsStringForRoom(Guid roomId)
    {
        return AllBookingsForUser
            .Where(booking => booking.State == RoomStatus.OptionBooked && booking.RoomId == roomId)
            .OrderBy(x => x.Date)
            .First().Date.ToString("dddd dd MMMM yyyy", CultureInfo.CreateSpecificCulture("nl-BE"));
    }

    private string GetEndDateAsStringForRoom(Guid roomId)
    {
        return AllBookingsForUser
            .Where(booking => booking.State == RoomStatus.OptionBooked && booking.RoomId == roomId)
            .OrderByDescending(x => x.Date)
            .First().Date.ToString("dddd dd MMMM yyyy", CultureInfo.CreateSpecificCulture("nl-BE"));
    }

    private async Task ToggleRoomState(DateTime day, string room, MouseEventArgs e)
    {
        _busy = true;
        await BookingService.UpdateRoomState(day, room, UserID);
        await UpdateUI();
    }

    private async Task GenerateDaysForMonth(DateTime month)
    {
        DaysInSelectedMonth.Clear();
        var startOfMonth = new DateTime(month.Year, month.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

        int dayOfWeekOffset = (int)startOfMonth.DayOfWeek - (int)DayOfWeek.Monday;
        dayOfWeekOffset = dayOfWeekOffset < 0 ? 6 : dayOfWeekOffset;

        for (int i = 0; i < dayOfWeekOffset; i++)
        {
            DaysInSelectedMonth.Add(DateTime.MinValue);
        }

        for (int day = 0; day < daysInMonth; day++)
        {
            DaysInSelectedMonth.Add(startOfMonth.AddDays(day));
        }

        await InvokeAsync(StateHasChanged);
    }

    private bool cantBeBooked(DateTime day, string room)
    {
        var roomForDay = RoomStates.FirstOrDefault(st => st.Date == day && st.RoomName == room);
        if (roomForDay == null)
        {
            return true; // If there's no information for the room, it should be considered unavailable.
        }

        bool isBookedBySomeoneElse = roomForDay.UserId != UserID && roomForDay.UserId != Guid.Empty || roomForDay.State != RoomStatus.Available && roomForDay.State != RoomStatus.OptionBooked;

        return _busy || DateTime.Today >= day || isBookedBySomeoneElse;
    }

    private string GetRoomState(DateTime day, string room)
    {
        return RoomStates.FirstOrDefault(st => st.Date == day && st.RoomName == room)?.State.ToString() ?? RoomStatus.Available.ToString();
    }

    private string GetTooltip(DateTime day, string room)
    {
        var state = GetRoomState(day, room);
        if (day < DateTime.Today)
        {
            return "You can't book a room in the past.";
        }
        if (day == DateTime.Today)
        {
            return "You can't book a room for today";
        }
        return state switch
        {
            "OptionBooked" => $"Option on {room}",
            "Booked" => $"{room} is booked",
            "Blocked" => $"{room} is blocked on this day",
            "Available" => $"Click to book {room}",
            _ => "This should not be possible"
        };
    }

    private async Task PreviousMonth()
    {
        SelectedMonth = SelectedMonth.AddMonths(-1);
        await GenerateDaysForMonth(SelectedMonth);
        await UpdateUI();
    }

    private async Task NextMonth()
    {
        SelectedMonth = SelectedMonth.AddMonths(1);
        await GenerateDaysForMonth(SelectedMonth);
        await UpdateUI();
    }

    public double TotalPrice()
    {
        var totalPrice = _bookingStateService.CalculateTotalPrice(AllBookingsForUser.ToList(), out var korting);
        Korting = korting;
        Disabled = totalPrice == 0;
        return totalPrice;
    }

    private void NavigateToPayment()
    {
        ArgumentNullException.ThrowIfNull(NavigationManager, nameof(NavigationManager));
        ArgumentNullException.ThrowIfNull(_bookingStateService, nameof(_bookingStateService));

        var optionBookedRooms = AllBookingsForUser.Where(x => x.State == RoomStatus.OptionBooked).ToList();
        _bookingStateService.SetOptionBookedRooms(optionBookedRooms, new List<double> { TotalPrice() });
        NavigationManager.NavigateTo("/Payment");
    }

    private bool CanBook()
    {
        return Disabled || _busy;
    }
    private double DetermineSeasonPrice(DateTime selectedMonth)
    {
        return _bookingStateService.DetermineSeasonPrice(selectedMonth);
    }
    private double DetermineReduction(int days)
    {
        return _bookingStateService.DetermineReduction(days);
    }

    private RoomPriceResult CalculatePriceForRoom(Guid roomId)
    {
        return _bookingStateService.CalculatePriceForRoom(AllBookingsForUser, roomId);
    }

    private int GetDaysForRoom(Guid roomId)
    {
        return _bookingStateService.GetDaysForRoom(AllBookingsForUser, roomId);
    }


}
