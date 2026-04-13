using GenderClassifierApi.Services;
using GenderClassifierApi.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Xunit;

public sealed class GenderizeServiceTests
{
    [Fact]
    public async Task ClassifyAsync_ReturnsProcessedSuccessPayload()
    {
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.Json("""
            { "name": "john", "gender": "male", "probability": 0.99, "count": 1234 }
            """));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/")
        };

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<GenderizeService>.Instance;

        var service = new GenderizeService(client, cache, logger);

        var (statusCode, payload) = await service.ClassifyAsync("john", CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        Assert.Equal(StatusCodes.Status200OK, statusCode);
        Assert.Contains("\"status\":\"success\"", json);
        Assert.Contains("\"sample_size\":1234", json);
        Assert.Contains("\"is_confident\":true", json);
        Assert.Contains("\"gender\":\"male\"", json);
        Assert.Contains("\"name\":\"john\"", json);
    }

    [Fact]
    public async Task ClassifyAsync_ReturnsEdgeCaseMessage_WhenGenderIsNull()
    {
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.Json("""
            { "name": "unknown", "gender": null, "probability": 0.0, "count": 0 }
            """));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/")
        };

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<GenderizeService>.Instance;

        var service = new GenderizeService(client, cache, logger);

        var (statusCode, payload) = await service.ClassifyAsync("unknown", CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, statusCode);
        Assert.Contains("No prediction available for the provided name", json);
    }

    [Fact]
    public async Task ClassifyAsync_ReturnsBadGateway_WhenUpstreamFails()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.genderize.io/")
        };

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<GenderizeService>.Instance;

        var service = new GenderizeService(client, cache, logger);

        var (statusCode, payload) = await service.ClassifyAsync("john", CancellationToken.None);

        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        Assert.Equal(StatusCodes.Status502BadGateway, statusCode);
        Assert.Contains("Upstream service returned an error", json);
    }
}