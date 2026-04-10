using GenderClassifierApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCors", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddHttpClient<IGenderizeService, GenderizeService>(client =>
{
    client.BaseAddress = new Uri("https://api.genderize.io/");
    client.Timeout = TimeSpan.FromSeconds(2);
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("OpenCors");

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        return Task.CompletedTask;
    });

    await next();
});

app.MapControllers();

app.Run();

public partial class Program { }
