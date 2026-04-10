namespace GenderClassifierApi.Services;

public interface IGenderizeService
{
    Task<(int StatusCode, object Payload)> ClassifyAsync(string name, CancellationToken cancellationToken);
}
