using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Shortener.Api;

public sealed class ShortenerService(IPersistenceStore store, IOptions<ShortenerOptions> options, TimeProvider clock)
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public async Task<LinkResponse> CreateAsync(CreateLinkRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ValidationException("url must be an absolute HTTP or HTTPS URL.");
        if (request.ExpiresAt is { } expiresAt && expiresAt <= clock.GetUtcNow())
            throw new ValidationException("expiresAt must be in the future.");
        var code = request.Alias is null ? await GenerateCodeAsync(cancellationToken) : ValidateAlias(request.Alias);
        var link = new ShortLink(code, uri.ToString(), clock.GetUtcNow(), request.ExpiresAt, []);
        await store.SaveAsync(link, cancellationToken);
        return ToResponse(link);
    }

    public async Task<RedirectResult> ResolveAsync(string code, string? ip, string? userAgent, CancellationToken cancellationToken)
    {
        var link = await store.FindAsync(code, cancellationToken) ?? throw new LinkNotFoundException("Short link was not found.");
        if (link.ExpiresAt is { } expiresAt && expiresAt <= clock.GetUtcNow()) throw new LinkExpiredException("Short link has expired.");
        var fingerprint = Fingerprint(ip, userAgent);
        await store.RecordClickAsync(link.Code, new ClickEvent(clock.GetUtcNow(), fingerprint), cancellationToken);
        return new RedirectResult(link.Url);
    }

    public async Task<AnalyticsResponse> GetAnalyticsAsync(string code, CancellationToken cancellationToken)
    {
        var link = await store.FindAsync(code, cancellationToken) ?? throw new LinkNotFoundException("Short link was not found.");
        return new AnalyticsResponse(link.Code, link.Clicks.Count, link.Clicks.Select(x => x.VisitorFingerprint).Distinct().LongCount(), link.Clicks.OrderByDescending(x => x.OccurredAt).Take(20).ToList());
    }

    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        var bytes = new byte[6];
        for (var attempt = 0; attempt < 5; attempt++)
        {
            RandomNumberGenerator.Fill(bytes);
            var code = new string(bytes.Select(x => Alphabet[x % Alphabet.Length]).ToArray());
            if (!await store.ExistsAsync(code, cancellationToken)) return code;
        }
        throw new ConflictException("Unable to allocate a unique short code.");
    }

    private static string ValidateAlias(string alias)
    {
        if (alias.Length is < 3 or > 32 || alias.Any(x => !(char.IsLetterOrDigit(x) || x is '-' or '_')))
            throw new ValidationException("alias must be 3-32 letters, digits, '-' or '_'.");
        return alias;
    }

    private LinkResponse ToResponse(ShortLink link) => new(link.Code, link.Url, $"{options.Value.PublicBaseUrl.TrimEnd('/')}/{link.Code}", link.CreatedAt, link.ExpiresAt);
    private static string Fingerprint(string? ip, string? agent) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ip ?? "unknown"}|{agent ?? "unknown"}")));
}
