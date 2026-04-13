using System.Text.Json.Serialization;

namespace GenderClassifierApi.Models;

using System.Text.Json.Serialization;

public sealed class ClassifyData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("sample_size")]
    public int SampleSize { get; set; }

    [JsonPropertyName("is_confident")]
    public bool IsConfident { get; set; }

    [JsonPropertyName("processed_at")]
    public string ProcessedAt { get; set; } = string.Empty;
}

public sealed class SuccessEnvelope
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "success";

    [JsonPropertyName("data")]
    public ClassifyData Data { get; set; } = new();
}

public sealed class ErrorEnvelope
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "error";

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public ErrorEnvelope(string message)
    {
        Message = message;
    }
}
