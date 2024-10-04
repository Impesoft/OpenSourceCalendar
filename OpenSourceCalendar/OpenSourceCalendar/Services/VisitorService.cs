using OpenSourceCalendar.Data;

public class VisitorService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _dbContext = context;

    public Guid GetVisitorId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));

        if (httpContext.Request.Cookies.TryGetValue("VisitorId", out var visitorIdValue) && Guid.TryParse(visitorIdValue, out var userID))
        {
            var dbUser = _dbContext.Users.FirstOrDefault(u => u.Id == userID.ToString());
            if (dbUser == null)
            {
                _dbContext.Users.Add(new ApplicationUser
                {
                    Id = userID.ToString(),
                    UserName = $"visitor_{userID}",
                    VerificationCode = "Not Yet Generated",
                    Email = null
                });
                _dbContext.SaveChanges();
            }
            return userID;
        }
        var visitorId = Guid.NewGuid();
        httpContext.Response.Cookies.Append("VisitorId", visitorId.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        // Check if visitor exists in the database, otherwise create a new user
        var user = _dbContext.Users.FirstOrDefault(u => u.Id == visitorId.ToString());
        if (user == null)
        {
            _dbContext.Users.Add(new ApplicationUser
            {
                Id = visitorId.ToString(),
                UserName = $"visitor_{visitorId}",
                VerificationCode = "Not Yet Generated",
                Email = null
            });
            _dbContext.SaveChanges();
        }

        return visitorId;
    }
    public ApplicationUser? GetVisitor(HttpContext httpContext)
    {
        var visitorId = GetVisitorId(httpContext);
        return _dbContext.Users.FirstOrDefault(u => u.Id == visitorId.ToString());
    }
}
