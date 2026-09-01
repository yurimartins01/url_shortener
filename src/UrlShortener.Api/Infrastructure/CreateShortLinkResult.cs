namespace UrlShortener.Api.Infrastructure
{
    public enum CreateShortLinkError
    {
        NullUrl,
        EmptyUrl,
        TooLongUrl,
        NotAnUrl,
        InvalidUrlScheme,
        MaxAttemptsReached
    }
    public record CreateShortLinkResult(
        bool Success, 
        string? Code = null,
        DateTime? CreatedAt = null,
        CreateShortLinkError? Error = null
        );

}
