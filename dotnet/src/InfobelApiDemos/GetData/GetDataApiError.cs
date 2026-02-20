namespace InfobelApiDemos.GetData;

public class GetDataApiError : Exception
{
    public GetDataApiError(string message) : base(message) { }
    public GetDataApiError(string message, Exception innerException) : base(message, innerException) { }
}
