using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace UrlShortener.Api.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var connectionString = context.Configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default não encontrada. Configure o user-secrets do projeto UrlShortener.Api antes de executar os testes.");
                
                NpgsqlConnectionStringBuilder psql = new(connectionString)
                {

                    Database = "urlshortener_test"
                };

                config.AddInMemoryCollection(new Dictionary<string, string?>()
                {
                    ["ConnectionStrings:Default"] = psql.ConnectionString
                });

            });

        }
    }
}
