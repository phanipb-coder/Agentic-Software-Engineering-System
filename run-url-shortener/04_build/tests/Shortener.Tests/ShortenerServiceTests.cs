using Microsoft.Extensions.Options;
using Shortener.Api;

namespace Shortener.Tests;

public sealed class ShortenerServiceTests
{
    [Fact]
    public async Task CreateLink_ValidUrl_ReturnsShortLink()
    {
        var service = CreateService();
        var result = await service.CreateAsync(new CreateLinkRequest("https://example.com", null, null), CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        Assert.Equal("https://example.com/", result.Url);
    }

    [Fact]
    public async Task CreateLink_InvalidUrl_ThrowsValidationException()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateLinkRequest("not-a-url", null, null), CancellationToken.None));
    }

    [Fact]
    public async Task ResolveLink_ActiveLink_ReturnsDestinationAndRecordsClick()
    {
        var service = CreateService();
        var created = await service.CreateAsync(new CreateLinkRequest("https://example.com/path", "demo", null), CancellationToken.None);
        var result = await service.ResolveAsync(created.Code, "127.0.0.1", "test", CancellationToken.None);
        var analytics = await service.GetAnalyticsAsync(created.Code, CancellationToken.None);
        Assert.Equal(created.Url, result.Url);
        Assert.Equal(1, analytics.TotalClicks);
    }

    private static ShortenerService CreateService() => new(new MemoryStore(), Options.Create(new ShortenerOptions()), TimeProvider.System);

    private sealed class MemoryStore : IPersistenceStore
    {
        private readonly Dictionary<string, ShortLink> links = new(StringComparer.OrdinalIgnoreCase);
        public Task<ShortLink?> FindAsync(string code, CancellationToken _) => Task.FromResult(links.GetValueOrDefault(code));
        public Task<bool> ExistsAsync(string code, CancellationToken _) => Task.FromResult(links.ContainsKey(code));
        public Task SaveAsync(ShortLink link, CancellationToken _) { if (links.ContainsKey(link.Code)) throw new ConflictException("duplicate"); links.Add(link.Code, link); return Task.CompletedTask; }
        public Task RecordClickAsync(string code, ClickEvent click, CancellationToken _) { links[code].Clicks.Add(click); return Task.CompletedTask; }
    }
}
