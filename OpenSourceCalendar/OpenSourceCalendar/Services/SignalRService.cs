using Microsoft.AspNetCore.SignalR;

namespace CasaAdelia.Services
{
    public class SignalRService : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
