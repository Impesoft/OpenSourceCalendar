namespace CasaAdelia.Services;

public interface ICookieService
{
    Task SetCookieAsync(string name, string value, int days);
}
