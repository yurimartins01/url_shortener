using Microsoft.EntityFrameworkCore;
using Npgsql;
using UrlShortener.Api.Contracts;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Infrastructure
{
    public class ShortLinkService
    {
        private readonly AppDbContext _context;
        private readonly IShortCodeGenerator _generator;
        private readonly ILogger<ShortLinkService> _logger;
        private const int MaxAttempts = 5;

        public ShortLinkService(AppDbContext context, IShortCodeGenerator generator, ILogger<ShortLinkService> logger)
        {
            _context = context;
            _generator = generator;
            _logger = logger;
        }

        public async Task<CreateShortLinkResult> CreateAsync(CreateShortLinkRequest request)
        {
            if (request.OriginalUrl is null)
            {
                return new(
                    Success: false,
                    Error: CreateShortLinkError.NullUrl);
            }
            if (request.OriginalUrl.Length < 1)
            {
                return new(
                    Success: false,
                    Error: CreateShortLinkError.EmptyUrl);
            }
            if (request.OriginalUrl.Length > 2048)
            {
                return new(
                    Success: false,
                    Error: CreateShortLinkError.TooLongUrl);
            }

            if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri))
            {
                return new(
                    Success: false,
                    Error: CreateShortLinkError.NotAnUrl);
            
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return new(
                    Success: false,
                    Error: CreateShortLinkError.InvalidUrlScheme);
            }


            int attempts = 1;

            while (MaxAttempts >= attempts)
            {
                ShortLink shortLinkEntity = new();
                try
                {
                    string code = _generator.Generate();

                    shortLinkEntity.Code = code;
                    shortLinkEntity.OriginalUrl = request.OriginalUrl;

                    _context.ShortLinks.Add(shortLinkEntity);
                    await _context.SaveChangesAsync();

                    return new(
                        Success: true,
                        Code: code,
                        CreatedAt: shortLinkEntity.CreatedAt);

                }
                catch (DbUpdateException e) when (e.InnerException is PostgresException pge && pge.SqlState == PostgresErrorCodes.UniqueViolation)
                {

                    _logger.LogWarning(pge, "Colisão detectada. Tentativas: ({Attempts}/{MaxAttempts}).", attempts, MaxAttempts);
                    _context.Entry(shortLinkEntity).State = EntityState.Detached;

                    attempts++;
                }
                
            }
            _logger.LogError("Falha em gerar um código único após {MaxAttempts} tentativas.", MaxAttempts);
            return new(
                    Success: false,
                    Error: CreateShortLinkError.MaxAttemptsReached);
        }

        public async Task<string?> GetAsync(string code)
        {
            return await _context.ShortLinks.Where(c => c.Code == code).Select(o => o.OriginalUrl).FirstOrDefaultAsync();
            
        }
    }
}
