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
                ?? throw new InvalidOperationException("ConnectionStrings:Default not found. Configure the user secrets of UrlShortener.Api project before executing the tests.");
                
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
