using System.Text.Json.Serialization;

namespace GenderClassifierApi.Models;

public sealed class ClassifyData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; init; } = string.Empty;

    [JsonPropertyName("probability")]
    public double Probability { get; init; }

    [JsonPropertyName("sample_size")]
    public int SampleSize { get; init; }

    [JsonPropertyName("is_confident")]
    public bool IsConfident { get; init; }

    [JsonPropertyName("processed_at")]
    public string ProcessedAt { get; init; } = string.Empty;
}

public sealed class SuccessEnvelope
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "success";

    [JsonPropertyName("data")]
    public ClassifyData Data { get; init; } = new();
}

public sealed class ErrorEnvelope
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "error";

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    public ErrorEnvelope() { }

    public ErrorEnvelope(string message)
    {
        Message = message;
    }
}
