using GestionClubs.Domain.Entities;

namespace Integration.Test;

public static class HttpClientExtensions
{
    public static HttpClient WithRole(this HttpClient client, params string[] roles)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Roles");
        client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        return client;
    }
}
