using OpenSourceCalendar.Types;

namespace CasaAdelia.Services
{
    public class BookingStateService
    {
        public List<double>? Prices { get; private set; }
        public List<RoomState>? OptionBookedRooms { get; private set; }

        public void SetOptionBookedRooms(List<RoomState> optionBookedRooms, List<double> prices)
        {
            OptionBookedRooms = optionBookedRooms;
            Prices = prices;
        }

        public double CalculateTotalPrice(List<RoomState> allBookingsForUser, out double totalDiscount)
        {
            double totalPrice = 0;
            totalDiscount = 0;

            foreach (Guid roomId in allBookingsForUser.Select(x => x.RoomId).Distinct())
            {
                var pricesForRoom = CalculatePriceForRoom(allBookingsForUser, roomId);
                totalPrice += pricesForRoom.TotalPrice;
                totalDiscount += pricesForRoom.Korting;
            }

            return totalPrice;
        }

        public RoomPriceResult CalculatePriceForRoom(IEnumerable<RoomState> allBookingsForUser, Guid roomId)
        {
            int days = GetDaysForRoom(allBookingsForUser, roomId);
            double dailyPrice;
            double discount;

            if (days > 0)
            {
                var startDate = allBookingsForUser
                    .OrderBy(st => st.Date)
                    .ThenBy(st => st.RoomName)
                    .FirstOrDefault(state => state.State == RoomStatus.OptionBooked && state.RoomId == roomId);

                dailyPrice = DetermineSeasonPrice(startDate?.Date ?? DateTime.Now);
                discount = dailyPrice * days * DetermineReduction(days);
            }
            else
            {
                dailyPrice = DetermineSeasonPrice(DateTime.Now);
                discount = dailyPrice * days * DetermineReduction(days);
            }

            return new RoomPriceResult(days * dailyPrice, dailyPrice, discount);
        }

        public int GetDaysForRoom(IEnumerable<RoomState> allBookingsForUser, Guid roomId)
        {
            return allBookingsForUser
                .Where(r => r.RoomId == roomId && r.State == RoomStatus.OptionBooked)
                .Count(); // Each entry represents one day, so we count the entries.
        }

        public double DetermineSeasonPrice(DateTime date)
        {
            bool isSeason = date.Month >= 6 && date.Month <= 9;
            return isSeason ? SeasonPrice : OffSeasonPrice;
        }

        public double DetermineReduction(int days)
        {
            return days switch
            {
                > 13 => 0.2,
                > 6 => 0.1,
                _ => 0,
            };
        }

        public double SeasonPrice { get; set; } = 100.0; // Example default value
        public double OffSeasonPrice { get; set; } = 80.0; // Example default value
    }

    //public class RoomPriceResult
    //{
    //    public double TotalPrice { get; }
    //    public double DailyPrice { get; }
    //    public double Korting { get; }

    //    public RoomPriceResult(double totalPrice, double dailyPrice, double korting)
    //    {
    //        TotalPrice = totalPrice;
    //        DailyPrice = dailyPrice;
    //        Korting = korting;
    //    }
    //}
}
