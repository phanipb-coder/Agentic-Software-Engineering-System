namespace Shortener.Api;

public sealed record CreateLinkRequest(string? Url, string? Alias, DateTimeOffset? ExpiresAt);
public sealed record LinkResponse(string Code, string Url, string ShortUrl, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt);
public sealed record ClickEvent(DateTimeOffset OccurredAt, string VisitorFingerprint);
public sealed record ShortLink(string Code, string Url, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, List<ClickEvent> Clicks);
public sealed record AnalyticsResponse(string Code, long TotalClicks, long UniqueVisitors, IReadOnlyList<ClickEvent> RecentEvents);
public sealed record RedirectResult(string Url);

public sealed class ShortenerOptions
{
    public string StoragePath { get; set; } = "data/shortener.json";
    public string PublicBaseUrl { get; set; } = "http://localhost:5080";
}

public sealed class ValidationException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class LinkNotFoundException(string message) : Exception(message);
public sealed class LinkExpiredException(string message) : Exception(message);
