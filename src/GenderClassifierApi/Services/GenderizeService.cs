using GenderClassifierApi.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace GenderClassifierApi.Services;

public sealed class GenderizeService : IGenderizeService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GenderizeService> _logger;


    public GenderizeService(HttpClient httpClient, IMemoryCache cache, ILogger<GenderizeService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(int StatusCode, object Payload)> ClassifyAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            _logger.LogWarning("Bad request: name parameter is missing or empty");
            return (StatusCodes.Status400BadRequest, new ErrorEnvelope("Missing or empty name parameter"));
        }

        if (!LooksLikeAName(normalizedName))
        {
            _logger.LogWarning("Unprocessable request: invalid name format received: {Name}", normalizedName);
            return (StatusCodes.Status422UnprocessableEntity, new ErrorEnvelope("name is not a string"));
        }

        var cacheKey = $"genderize:{normalizedName.ToLowerInvariant()}";

        GenderizeResponse? upstream;

        if (!_cache.TryGetValue(cacheKey, out upstream))
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"?name={Uri.EscapeDataString(normalizedName)}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Genderize API returned non-success status code: {StatusCode}",
                        (int)response.StatusCode);

                    return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Upstream service returned an error"));
                }

                upstream = await response.Content.ReadFromJsonAsync<GenderizeResponse>(
                    cancellationToken: cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Genderize API request timed out");
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Upstream request timed out"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Genderize API request failed");
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Upstream request failed"));
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Failed to parse Genderize API response");
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Failed to parse upstream response"));
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON returned from Genderize API");
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Failed to parse upstream response"));
            }

            if (upstream is null)
            {
                _logger.LogError("Genderize API response was null");
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Failed to parse upstream response"));
            }

            if (string.IsNullOrWhiteSpace(upstream.Gender) || upstream.Count == 0)
            {
                return (StatusCodes.Status422UnprocessableEntity, new ErrorEnvelope("No prediction available for the provided name"));
            }

            _cache.Set(
                cacheKey,
                upstream,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
        }

        if (string.IsNullOrWhiteSpace(upstream?.Gender) || upstream.Count == 0)
        {
            return (StatusCodes.Status422UnprocessableEntity, new ErrorEnvelope("No prediction available for the provided name"));
        }

        var probability = upstream.Probability ?? 0.0d;
        bool confidenceCondition = probability >= 0.7d && upstream.Count >= 100;

        var data = new ClassifyData
        {
            Name = normalizedName,
            Gender = upstream.Gender,
            Probability = probability,
            SampleSize = upstream.Count,
            IsConfident = confidenceCondition,
            ProcessedAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
        };

        return (StatusCodes.Status200OK, new SuccessEnvelope { Data = data });
    }

    private static bool LooksLikeAName(string value)
    {
        var pattern = @"^[\p{L}\p{M}][\p{L}\p{M}\s'\-]*$";

        return Regex.IsMatch(value, pattern);
    }
}
