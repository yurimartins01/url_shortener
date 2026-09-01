using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;
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
                    var errors = new Dictionary<string, string[]>();

                    switch(result.Error)
                    {
                        case CreateShortLinkError.NullUrl:
                            errors.Add("originalUrl", ["A URL não pode ser nula"]);
                            break;

                        case CreateShortLinkError.EmptyUrl:
                            errors.Add("originalUrl", ["A URL não pode ser vazia."]);
                            break;

                        case CreateShortLinkError.TooLongUrl: 
                            errors.Add("originalUrl", ["A URL deve ser igual ou menor a 2048 caracteres."]);
                            break;

                        case CreateShortLinkError.NotAnUrl: 
                            errors.Add("originalUrl", ["Formato inválido, apenas URLs são aceitas."]);
                            break;

                        case CreateShortLinkError.InvalidUrlScheme: 
                            errors.Add("originalUrl", ["A URL é inválida, apenas esquemas HTTP e HTTPS são aceitos."]);
                            break;

                        case CreateShortLinkError.MaxAttemptsReached:
                            
                            return Results.Problem(
                                title: "Falha na criação do código da URL curta.",
                                detail: "Número máximo de tentativas atingido.",
                                statusCode: (int)HttpStatusCode.InternalServerError);

                        default: return Results.Problem();

                    };

                    return Results.ValidationProblem(
                        errors: errors,
                        title: "Falha na validação da URL"
                    );
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
