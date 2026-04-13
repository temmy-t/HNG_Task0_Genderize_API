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

    public async Task<(int StatusCode, object Payload)> ClassifyAsync(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (StatusCodes.Status400BadRequest, new ErrorEnvelope("Missing or empty name parameter"));
        }

        var normalizedName = name.Trim();

        if (!IsValidName(normalizedName))
        {
            return (StatusCodes.Status422UnprocessableEntity, new ErrorEnvelope("name is not a string"));
        }

        var cacheKey = $"genderize:{normalizedName.ToLowerInvariant()}";

        if (!_cache.TryGetValue(cacheKey, out GenderizeResponse? upstream))
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

        if (string.IsNullOrWhiteSpace(upstream.Gender) || upstream.Count == 0)
        {
            return (StatusCodes.Status422UnprocessableEntity, new ErrorEnvelope("No prediction available for the provided name"));
        }

        var probability = upstream.Probability ?? 0.0d;

        var data = new ClassifyData
        {
            Name = normalizedName,
            Gender = upstream.Gender,
            Probability = probability,
            SampleSize = upstream.Count,
            IsConfident = probability >= 0.7d && upstream.Count >= 100,
            ProcessedAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
        };

        return (StatusCodes.Status200OK, new SuccessEnvelope
        {
            Status = "success",
            Data = data
        });
    }

    private static bool IsValidName(string value)
    {
        // Accepts names like john, Mary Jane, O'Neil, Jean-Luc
        // Rejects digit-only or symbol-only values like 123
        return value.Any(char.IsLetter) &&
               value.All(ch => char.IsLetter(ch) || char.IsWhiteSpace(ch) || ch is '-' or '\'');
    }
}
