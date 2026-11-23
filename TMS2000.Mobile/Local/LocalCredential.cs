using SQLite;

namespace TMS2000.Mobile.Local;

public class LocalCredential
{
    [PrimaryKey]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime LastOnlineLoginAt { get; set; }
}
