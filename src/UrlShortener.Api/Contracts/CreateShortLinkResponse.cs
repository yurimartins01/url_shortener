namespace UrlShortener.Api.Contracts;

public record CreateShortLinkResponse(string Code, string ShortUrl, DateTime CreatedAt);