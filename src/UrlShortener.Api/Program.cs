using Microsoft.EntityFrameworkCore;
using Npgsql;
using UrlShortener.Api.Contracts;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention()
);

builder.Services.AddSingleton<IShortCodeGenerator, Base62ShortCodeGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/short-url", async (CreateShortLinkRequest request, IShortCodeGenerator generator, AppDbContext context, IConfiguration configuration, ILogger<Program> logger) =>
{
    if (request.OriginalUrl is null) return Results.BadRequest("Original Url should no be null.");
    if (request.OriginalUrl.Length > 2048) return Results.BadRequest("OriginalUrl has length greater than field capacity: 2048");

    if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri)) return Results.BadRequest("The given text is invalid. Only URLs are accepted.");

    if (uri.Scheme != "http" && uri.Scheme != "https")
    {
        return Results.BadRequest("The given URL is invalid. Only http and https schemes are accepted.");
    }

    int attempts = 1;
    int maxAttempts = 5;
    var url = configuration.GetValue<string>("BaseUrl");

    if (string.IsNullOrWhiteSpace(url) || !url.EndsWith('/')) return Results.InternalServerError("The base URL is not defined, missing '/' or empty in the application");

    while (maxAttempts >= attempts)
    {
        ShortLink shortLinkEntity = new();
        try
        {
            string code = generator.Generate();
            string shortUrl = url + code;

            shortLinkEntity.Code = code;
            shortLinkEntity.OriginalUrl = request.OriginalUrl;

            context.ShortLinks.Add(shortLinkEntity);
            await context.SaveChangesAsync();

            CreateShortLinkResponse response = new(code, shortUrl, shortLinkEntity.CreatedAt);

            return Results.Created(response.ShortUrl, response);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException pge && pge.SqlState == PostgresErrorCodes.UniqueViolation)
        {

            logger.LogWarning(pge, "Collision of code detected. Attempts: ({Attempts}/{MaxAttempts}).", attempts, maxAttempts);
            context.Entry(shortLinkEntity).State = EntityState.Detached;

            attempts++;

        }
    }
    logger.LogError("Failed to generate a unique code after {MaxAttempts} attempts", maxAttempts);
    return Results.InternalServerError("The attempt limit has been reached.");
});

app.MapGet("/{code:regex(^[a-zA-Z0-9]{{7}}$)}", async (string code, AppDbContext context) =>
{
    var originalUrl = await context.ShortLinks.Where(c => c.Code == code).Select(o => o.OriginalUrl).FirstOrDefaultAsync();

    if (originalUrl is null) return Results.NotFound("Short link not found.");

    return Results.Redirect(originalUrl);
});

app.Run();