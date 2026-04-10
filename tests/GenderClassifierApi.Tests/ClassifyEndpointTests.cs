using GenderClassifierApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace GenderClassifierApi.Tests;

public sealed class ClassifyEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ClassifyEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetClassify_Returns400_WhenNameIsMissing()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/classify");

        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task GetClassify_Returns422_WhenNameIsNotAStringLikeValue()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/classify?name=12345");

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, (int)response.StatusCode);
    }

    [Fact]
    public async Task GetClassify_ReturnsCorsHeader()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/classify?name=john");

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Contains("*", values);
    }
}

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGenderizeService>();
            services.AddSingleton<IGenderizeService, FakeGenderizeService>();
        });
    }
}

internal sealed class FakeGenderizeService : IGenderizeService
{
    public Task<(int StatusCode, object Payload)> ClassifyAsync(string name, CancellationToken cancellationToken)
    {
        if (name == "john")
        {
            var payload = new
            {
                status = "success",
                data = new
                {
                    name = "john",
                    gender = "male",
                    probability = 0.99,
                    sample_size = 1234,
                    is_confident = true,
                    processed_at = "2026-04-01T12:00:00Z"
                }
            };

            return Task.FromResult((StatusCodes.Status200OK, (object)payload));
        }

        var error = new
        {
            status = "error",
            message = "name is not a string"
        };

        return Task.FromResult((StatusCodes.Status422UnprocessableEntity, (object)error));
    }
}
