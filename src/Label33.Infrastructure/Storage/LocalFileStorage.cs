using Label33.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Label33.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var key = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, cancellationToken);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar));
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }
}

public sealed class FileEmailSender : IEmailSender
{
    private readonly string _root;
    private readonly ILogger<FileEmailSender> _logger;

    public FileEmailSender(string root, ILogger<FileEmailSender> logger)
    {
        _root = root;
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var safeTo = string.Join("_", (to ?? "unknown").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var path = Path.Combine(_root, $"{stamp}__{safeTo}.txt");
        var content = $"To: {to}\nSubject: {subject}\nUtc: {DateTime.UtcNow:O}\n\n{body}\n";
        await File.WriteAllTextAsync(path, content, cancellationToken);
        _logger.LogInformation("Email queued to file {Path} for {To}: {Subject}", path, to, subject);
    }
}

public sealed class NullEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
