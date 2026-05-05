using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PowerBiProxy;

public class ApiBasedService(IHttpClientFactory httpClientFactory, PowerBiSettings settings)
{
    private static readonly string PowerBiScope = "https://analysis.windows.net/powerbi/api/.default";
    private static readonly string PowerBiApiBase = "https://api.powerbi.com/v1.0/myorg";

    private const string ColumnProjection = """
        "AccountID",              ReportDataDev[AccountID],
        "AccountName",            ReportDataDev[AccountName],
        "Adv Bucket",             ReportDataDev[Adv Bucket],
        "Advance Purchase",       ReportDataDev[Advance Purchase],
        "APG",                    ReportDataDev[APG],
        "Arrive City",            ReportDataDev[Arrive City],
        "Booking Source",         ReportDataDev[Booking Source],
        "Booking Type",           ReportDataDev[Booking Type],
        "Bookingdate",            ReportDataDev[Bookingdate],
        "BookingTypeName",        ReportDataDev[BookingTypeName],
        "Cabin",                  ReportDataDev[Cabin],
        "City Pair",              ReportDataDev[City Pair],
        "COL DSAID",              ReportDataDev[COL DSAID],
        "DataSourceID",           ReportDataDev[DataSourceID],
        "Depart City",            ReportDataDev[Depart City],
        "Domestic International", ReportDataDev[Domestic International],
        "EndDate",                ReportDataDev[EndDate]
        """;

    public Task<JsonNode> GetAllAsync(string datasetId) =>
        ExecuteDaxAsync($"EVALUATE TOPN(10, SELECTCOLUMNS(ReportDataDev, {ColumnProjection}))", datasetId);

    public Task<JsonNode> FilterByDataSourceIdAsync(string datasetId, string dataSourceId) =>
        ExecuteDaxAsync($"""
            EVALUATE
            SELECTCOLUMNS(
                FILTER(ReportDataDev, ReportDataDev[DataSourceID] = {dataSourceId}),
                {ColumnProjection}
            )
            """, datasetId);

    public Task<JsonNode> GetDistinctDataSourceIdsAsync(string datasetId) =>
        ExecuteDaxAsync("EVALUATE DISTINCT(SELECTCOLUMNS(ReportDataDev, \"DataSourceID\", ReportDataDev[DataSourceID]))", datasetId);

    public Task<JsonNode> GetAccountIdsByDataSourceIdAsync(string datasetId, string dataSourceId) =>
        ExecuteDaxAsync($"""
            EVALUATE
            DISTINCT(
                SELECTCOLUMNS(
                    FILTER(ReportDataDev, ReportDataDev[DataSourceID] = {dataSourceId}),
                    "AccountID", ReportDataDev[AccountID]
                )
            )
            """, datasetId);

    internal async Task<string> GetAccessTokenAsync()
    {
        var client = httpClientFactory.CreateClient("entra");

        var body = new FormUrlEncodedContent([
            new("grant_type",    "client_credentials"),
            new("client_id",     settings.ClientId),
            new("client_secret", settings.ClientSecret),
            new("scope",         PowerBiScope),
        ]);

        var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{settings.TenantId}/oauth2/v2.0/token",
            body);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Token error {(int)response.StatusCode}: {err}");
        }

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        return json["access_token"]!.GetValue<string>();
    }

    internal async Task<string> GenerateEmbedTokenAsync(
        string spToken, string datasetId, string username, string[] roles, string customData)
    {
        var client = httpClientFactory.CreateClient("powerbi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", spToken);

        var body = JsonSerializer.Serialize(new
        {
            accessLevel = "View",
            identities  = new[]
            {
                new { username, roles, customData, datasets = new[] { datasetId } }
            }
        });

        var url = $"{PowerBiApiBase}/groups/{settings.WorkspaceId}/datasets/{datasetId}/GenerateToken";
        var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"GenerateToken error {(int)response.StatusCode} {response.ReasonPhrase}: {err}");
        }

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        return json["token"]!.GetValue<string>();
    }

    internal async Task<JsonNode> ExecuteDaxWithTokenAsync(string daxQuery, string datasetId, string bearerToken)
    {
        var client = httpClientFactory.CreateClient("powerbi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var requestBody = BuildRequestBody(daxQuery, null);
        var url = $"{PowerBiApiBase}/groups/{settings.WorkspaceId}/datasets/{datasetId}/executeQueries";
        var response = await client.PostAsync(url, new StringContent(requestBody, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"PowerBI API error {(int)response.StatusCode} {response.ReasonPhrase}: {err}");
        }

        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    internal async Task<JsonNode> ExecuteDaxAsync(string daxQuery, string datasetId, object? extraRequestFields = null)
    {
        var token = await GetAccessTokenAsync();
        var client = httpClientFactory.CreateClient("powerbi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var requestBody = BuildRequestBody(daxQuery, extraRequestFields);

        var url = $"{PowerBiApiBase}/groups/{settings.WorkspaceId}/datasets/{datasetId}/executeQueries";
        var response = await client.PostAsync(url, new StringContent(requestBody, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"PowerBI API error {(int)response.StatusCode} {response.ReasonPhrase}: {err}");
        }

        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    private static string BuildRequestBody(string daxQuery, object? extraFields)
    {
        var doc = new JsonObject
        {
            ["queries"] = new JsonArray(new JsonObject { ["query"] = daxQuery }),
            ["serializerSettings"] = new JsonObject { ["includeNulls"] = true }
        };

        if (extraFields is not null)
        {
            foreach (var prop in JsonSerializer.SerializeToElement(extraFields).EnumerateObject())
                doc[prop.Name] = JsonNode.Parse(prop.Value.GetRawText());
        }

        return doc.ToJsonString();
    }
}
