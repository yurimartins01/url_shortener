using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using UrlShortener.Api.Contracts;

namespace UrlShortener.Api.Tests
{
    public class ShortLinkFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _http;
        private readonly CreateShortLinkRequest _request = new("https://example.com");


        public ShortLinkFlowTests(CustomWebApplicationFactory factory)
        {
            _http = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        [Fact]
        public async Task POST_WhenValidURL_ReturnsStatusCode201()
        {

            var response = await _http.PostAsJsonAsync("/api/short-url", _request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        }

        [Fact]
        public async Task GET_WhenCodeExists_ReturnsStatusCode302()
        {

            var postResponse = await _http.PostAsJsonAsync("/api/short-url", _request);

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var result = await postResponse.Content.ReadFromJsonAsync<CreateShortLinkResponse>();


            var getResponse = await _http.GetAsync($"/{result!.Code}");

            Uri originalUrl = new(_request.OriginalUrl);

            Assert.Equal(HttpStatusCode.Found, getResponse.StatusCode);
            Assert.Equal(originalUrl, getResponse.Headers.Location);

        }

    }
}
