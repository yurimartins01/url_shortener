using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Contracts;
using UrlShortener.Api.Infrastructure;

namespace UrlShortener.Api.Tests.Integration
{
    public class ShortLinkServiceRetryTests : IClassFixture<FakeGeneratorWebApplicationFactory>
    {
        private readonly FakeGeneratorWebApplicationFactory _factory;

        public ShortLinkServiceRetryTests(FakeGeneratorWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateAsync_WhenCodeAlwaysCollides_ReturnsMaxAttemptsReached()
        {
            CreateShortLinkRequest request = new("https://www.example.com");
            CreateShortLinkResult firstResult;
            CreateShortLinkResult secondResult;

            { 

                using var scope = _factory.Services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ShortLinkService>();

                firstResult = await service.CreateAsync(request);

            }

            Assert.True(firstResult.Success);

            {
                using var scope = _factory.Services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ShortLinkService>();

                secondResult = await service.CreateAsync(request);
            }

            Assert.False(secondResult.Success);
            Assert.Equal(CreateShortLinkError.MaxAttemptsReached, secondResult.Error);

        }

    }
}
