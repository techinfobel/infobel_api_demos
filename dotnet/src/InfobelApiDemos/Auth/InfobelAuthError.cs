namespace InfobelApiDemos.Auth;

public class InfobelAuthError : Exception
{
    public InfobelAuthError(string message) : base(message) { }
    public InfobelAuthError(string message, Exception innerException) : base(message, innerException) { }
}
