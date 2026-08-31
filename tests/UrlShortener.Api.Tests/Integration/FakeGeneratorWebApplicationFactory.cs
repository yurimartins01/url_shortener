using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Tests.Integration
{
    public class FakeGeneratorWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.Replace(new ServiceDescriptor(
                    typeof(IShortCodeGenerator),
                    typeof(FakeShortCodeGenerator),
                    ServiceLifetime.Singleton));
            });
        }
    }
}
