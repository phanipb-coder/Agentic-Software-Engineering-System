using Microsoft.Extensions.Options;
using Shortener.Api;

namespace Shortener.Tests;

public sealed class FailurePathTests
{
    [Fact]
    public async Task CreateLink_DuplicateAlias_ThrowsConflict()
    {
        var store = new TestStore();
        var service = new ShortenerService(store, Options.Create(new ShortenerOptions()), TimeProvider.System);
        await service.CreateAsync(new CreateLinkRequest("https://example.com", "demo", null), CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateLinkRequest("https://contoso.com", "demo", null), CancellationToken.None));
    }

    [Fact]
    public async Task ResolveLink_ExpiredLink_ThrowsGoneException()
    {
        var store = new TestStore();
        var clock = new FixedTimeProvider(DateTimeOffset.Parse("2030-01-02T00:00:00Z"));
        var service = new ShortenerService(store, Options.Create(new ShortenerOptions()), clock);
        await store.SaveAsync(new ShortLink("old", "https://example.com", clock.GetUtcNow().AddDays(-1), clock.GetUtcNow().AddMinutes(-1), []), CancellationToken.None);
        await Assert.ThrowsAsync<LinkExpiredException>(() => service.ResolveAsync("old", null, null, CancellationToken.None));
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class TestStore : IPersistenceStore
    {
        private readonly Dictionary<string, ShortLink> links = new(StringComparer.OrdinalIgnoreCase);
        public Task<ShortLink?> FindAsync(string code, CancellationToken _) => Task.FromResult(links.GetValueOrDefault(code));
        public Task<bool> ExistsAsync(string code, CancellationToken _) => Task.FromResult(links.ContainsKey(code));
        public Task SaveAsync(ShortLink link, CancellationToken _) { if (links.ContainsKey(link.Code)) throw new ConflictException("duplicate"); links.Add(link.Code, link); return Task.CompletedTask; }
        public Task RecordClickAsync(string code, ClickEvent click, CancellationToken _) { links[code].Clicks.Add(click); return Task.CompletedTask; }
    }
}
