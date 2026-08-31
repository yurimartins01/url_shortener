using UrlShortener.Api.Contracts;
using UrlShortener.Api.Infrastructure;

namespace UrlShortener.Api.Endpoints
{
    public static class ShortLinkEndpoints
    {
        public static void Map(WebApplication app)
        {
            app.MapPost("/api/short-url", async (CreateShortLinkRequest request, IConfiguration configuration, ShortLinkService service) =>
            {

                var result = await service.CreateAsync(request);

                if (!result.Success)
                {
                    return result.Error switch
                    {
                        CreateShortLinkError.NullUrl => Results.BadRequest("A URL não pode ser nula"),

                        CreateShortLinkError.EmptyUrl => Results.BadRequest("A URL não pode ser vazia."),

                        CreateShortLinkError.TooLongUrl => Results.BadRequest("A URL deve ser igual ou menor a 2048 caracteres."),

                        CreateShortLinkError.NotAnUrl => Results.BadRequest("Formato inválido, apenas URLs são aceitas."),

                        CreateShortLinkError.InvalidUrlScheme => Results.BadRequest("A URL é inválida, apenas esquemas HTTP e HTTPS são aceitos."),

                        CreateShortLinkError.MaxAttemptsReached => Results.InternalServerError("Número máximo de tentativas atingido."),

                        _ => Results.InternalServerError()

                    };
                }

                var url = configuration.GetValue<string>("BaseUrl");

                if (string.IsNullOrWhiteSpace(url) || !url.EndsWith('/')) return Results.InternalServerError("A URL base não está definida, falta '/' ou está vazia na aplicação.");

                string shortUrl = url + result.Code;

                CreateShortLinkResponse response = new(result.Code!, shortUrl, (DateTime)result.CreatedAt!);

                return Results.Created(response.ShortUrl, response);
                
            });

            app.MapGet("/{code:regex(^[a-zA-Z0-9]{{7}}$)}", async (string code, ShortLinkService service) =>
            {
                var originalUrl = await service.GetAsync(code);

                if (originalUrl is null) return Results.NotFound("Link curto não encontrado.");

                return Results.Redirect(originalUrl);
            });
        }
    }
}
