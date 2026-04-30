using IdentityProvider.Abstracts;
using IdentityProvider.IdentityProviderTests.Endpoints;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace IdentityProvider.Tests.Endpoints
{
    [Collection("WebApplicationFactory")]
    public class WellKnownTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public WellKnownTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task OpenIdConfiguration_ReturnsOkWithExpectedFields()
        {
            // Act
            var response = await _client.GetAsync("/.well-known/openid-configuration");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("issuer", out var issuer));
            Assert.False(string.IsNullOrWhiteSpace(issuer.GetString()));

            Assert.True(json.TryGetProperty("jwks_uri", out var jwksUri));
            Assert.Contains(".well-known/jwks", jwksUri.GetString());

            Assert.True(json.TryGetProperty("token_endpoint", out var tokenEndpoint));
            Assert.Contains("connect/token", tokenEndpoint.GetString());
        }

        [Fact]
        public async Task Jwks_ReturnsOkWithRsaKey()
        {
            // Act
            var response = await _client.GetAsync("/.well-known/jwks");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("keys", out var keys));

            var keyArray = keys.EnumerateArray().ToList();
            Assert.Single(keyArray);

            var key = keyArray[0];
            Assert.Equal("RSA", key.GetProperty("kty").GetString());
            Assert.Equal("sig", key.GetProperty("use").GetString());
            Assert.Equal("1", key.GetProperty("kid").GetString());
            Assert.False(string.IsNullOrWhiteSpace(key.GetProperty("e").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(key.GetProperty("n").GetString()));
        }
    }

    [CollectionDefinition("WellKnown")]
    public class WellKnownCollection : ICollectionFixture<CustomWebApplicationFactory>
    {
    }
}