namespace TMS2000.Mobile.Services;

public class OfflineLoginException : Exception
{
    public OfflineLoginException(string message) : base(message)
    {
    }
}
