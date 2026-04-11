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
            return (StatusCodes.Status400BadRequest, new ErrorEnvelope("Missing or empty name parameter"));
        }

        if (!LooksLikeAName(normalizedName))
        {
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
                    return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Upstream service returned an error"));
                }

                upstream = await response.Content.ReadFromJsonAsync<GenderizeResponse>(
                    cancellationToken: cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Upstream request timed out"));
            }
            catch (HttpRequestException)
            {
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Upstream request failed"));
            }
            catch (NotSupportedException)
            {
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Failed to parse upstream response"));
            }
            catch (System.Text.Json.JsonException)
            {
                return (StatusCodes.Status502BadGateway, new ErrorEnvelope("Failed to parse upstream response"));
            }

            if (upstream is null)
            {
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
        string utcTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        var data = new ClassifyData
        {
            Name = normalizedName,
            Gender = upstream.Gender,
            Probability = probability,
            SampleSize = upstream.Count,
            IsConfident = confidenceCondition,
            ProcessedAt = utcTime
        };

        return (StatusCodes.Status200OK, new SuccessEnvelope { Data = data });
    }
    
    private static bool LooksLikeAName(string value)
    {
        var pattern = @"^[\p{L}\p{M}][\p{L}\p{M}\s'\-]*$";

        return Regex.IsMatch(value, pattern);
    }
}
