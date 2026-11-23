namespace TMS2000.Mobile.Local;

public class CurrentAccount
{
    private const string EmailPreferenceKey = "current_account_email";

    public CurrentAccount()
    {
        string savedEmail = Preferences.Default.Get(EmailPreferenceKey, string.Empty);
        Email = savedEmail.Length == 0 ? null : savedEmail;
    }

    public string? Email { get; private set; }

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public void SetEmail(string email)
    {
        string normalizedEmail = Normalize(email);
        if (normalizedEmail == Email)
        {
            return;
        }

        Email = normalizedEmail;
        Preferences.Default.Set(EmailPreferenceKey, normalizedEmail);
    }
}
