namespace OpenSourceCalendar.Types;

public class BookingInfo
{
    public string RecipientEmail { get; set; }
    public string UserName { get; set; }
    public Guid UserId { get; set; }
    public bool NewsLetter { get; set; }
    public string FromEmail { get; set; } = "DoNotReply@casa-adelia.com";
    //public string FromEmail { get; set; } = "DoNotReply@bae5a326-1602-45de-8ede-071d8f3cdd57.azurecomm.net";
    public string BookingConfirmedText { get; set; }
    public string BookingConfirmedHtml { get; set; }
    public string VerificationCode { get; set; }
}
