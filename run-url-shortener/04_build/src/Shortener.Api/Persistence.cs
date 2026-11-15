using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Shortener.Api;

public interface IPersistenceStore
{
    Task<ShortLink?> FindAsync(string code, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken);
    Task SaveAsync(ShortLink link, CancellationToken cancellationToken);
    Task RecordClickAsync(string code, ClickEvent click, CancellationToken cancellationToken);
}

public sealed class JsonFilePersistenceStore(IOptions<ShortenerOptions> options, ILogger<JsonFilePersistenceStore> logger) : IPersistenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path = Path.GetFullPath(options.Value.StoragePath);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<ShortLink?> FindAsync(string code, CancellationToken cancellationToken)
    {
        var links = await ReadAsync(cancellationToken);
        return links.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ExistsAsync(string code, CancellationToken cancellationToken) => await FindAsync(code, cancellationToken) is not null;

    public async Task SaveAsync(ShortLink link, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var links = await ReadUnsafeAsync(cancellationToken);
            if (links.Any(x => string.Equals(x.Code, link.Code, StringComparison.OrdinalIgnoreCase)))
                throw new ConflictException("The short code is already in use.");
            links.Add(link);
            await WriteUnsafeAsync(links, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task RecordClickAsync(string code, ClickEvent click, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var links = await ReadUnsafeAsync(cancellationToken);
            var link = links.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))
                ?? throw new LinkNotFoundException("Short link was not found.");
            link.Clicks.Add(click);
            await WriteUnsafeAsync(links, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private async Task<List<ShortLink>> ReadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return await ReadUnsafeAsync(cancellationToken); }
        finally { _gate.Release(); }
    }

    private async Task<List<ShortLink>> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return [];
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<ShortLink>>(stream, JsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteUnsafeAsync(List<ShortLink> links, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporary = _path + ".tmp";
        await using (var stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, links, JsonOptions, cancellationToken);
        File.Move(temporary, _path, true);
        logger.LogDebug("Persisted {LinkCount} links", links.Count);
    }
}
