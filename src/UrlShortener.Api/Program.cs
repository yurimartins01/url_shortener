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

{ 

    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();

}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/short-url", async (CreateShortLinkRequest request, IShortCodeGenerator generator, AppDbContext context, IConfiguration configuration, ILogger<Program> logger) =>
{
    if (request.OriginalUrl is null) return Results.BadRequest("A URL não pode ser nula");
    if (request.OriginalUrl.Length < 1) return Results.BadRequest("A URL não pode ser vazia.");
    if (request.OriginalUrl.Length > 2048) return Results.BadRequest("A URL deve ser igual ou menor a 2048 caracteres.");

    if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri)) return Results.BadRequest("Formato inválido, apenas URLs são aceitas.");

    if (uri.Scheme != "http" && uri.Scheme != "https")
    {
        return Results.BadRequest("A URL é inválida, apenas esquemas HTTP e HTTPS são aceitos.");
    }

    int attempts = 1;
    int maxAttempts = 5;
    var url = configuration.GetValue<string>("BaseUrl");

    if (string.IsNullOrWhiteSpace(url) || !url.EndsWith('/')) return Results.InternalServerError("A URL base não está definida, falta '/' ou está vazia na aplicação.");

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

            logger.LogWarning(pge, "Colisão detectada. Tentivas: ({Attempts}/{MaxAttempts}).", attempts, maxAttempts);
            context.Entry(shortLinkEntity).State = EntityState.Detached;

            attempts++;

        }
    }
    logger.LogError("Falha em gerar um código único após {MaxAttempts} tentativas.", maxAttempts);
    return Results.InternalServerError("Número máximo de tentativas atingido.");
});

app.MapGet("/{code:regex(^[a-zA-Z0-9]{{7}}$)}", async (string code, AppDbContext context) =>
{
    var originalUrl = await context.ShortLinks.Where(c => c.Code == code).Select(o => o.OriginalUrl).FirstOrDefaultAsync();

    if (originalUrl is null) return Results.NotFound("Link curto não encontrado.");

    return Results.Redirect(originalUrl);
});

app.Run();