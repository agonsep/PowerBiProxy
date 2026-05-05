using Azure.Core;
using Azure.Identity;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;

namespace PowerBiProxy;

public class PowerBiClientFactory(PowerBiSettings settings)
{
    private static readonly string[] Scopes = ["https://analysis.windows.net/powerbi/api/.default"];
    private static readonly Uri PowerBiBaseUri = new("https://api.powerbi.com/");

    public async Task<PowerBIClient> CreateAsync()
    {
        var credential = new ClientSecretCredential(
            settings.TenantId,
            settings.ClientId,
            settings.ClientSecret);

        var token = await credential.GetTokenAsync(new TokenRequestContext(Scopes));

        return new PowerBIClient(PowerBiBaseUri, new TokenCredentials(token.Token, "Bearer"));
    }
}
