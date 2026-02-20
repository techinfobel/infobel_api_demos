namespace InfobelApiDemos.BizSearch;

public class BizSearchApiError : Exception
{
    public BizSearchApiError(string message) : base(message) { }
    public BizSearchApiError(string message, Exception innerException) : base(message, innerException) { }
}
