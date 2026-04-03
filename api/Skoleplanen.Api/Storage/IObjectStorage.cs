namespace Skoleplanen.Api.Storage;

public interface IObjectStorage
{
    Task<string> UploadAsync(string key, string contentType, Stream content, CancellationToken ct = default);
}
