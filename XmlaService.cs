using Microsoft.AnalysisServices.AdomdClient;
using System.Text.Json.Nodes;

namespace PowerBiProxy;

public class XmlaService(PowerBiSettings settings)
{
    // datasetName  — the display name of the semantic model in Power BI (not the GUID)
    // customData   — value passed to CUSTOMDATA() in the RLS filter
    public async Task<JsonNode> ExecuteDaxAsync(string dax, string datasetName, string customData)
    {
        var connectionString = BuildConnectionString(datasetName, customData);

        // AdomdConnection is synchronous; run on a thread-pool thread
        return await Task.Run(() =>
        {
            using var connection = new AdomdConnection(connectionString);
            connection.Open();

            using var command = new AdomdCommand(dax, connection);
            using var reader  = command.ExecuteReader();

            return ReadToJson(reader);
        });
    }

    private string BuildConnectionString(string datasetName, string customData) =>
        $"Data Source=powerbi://api.powerbi.com/v1.0/myorg/{Uri.EscapeDataString(settings.WorkspaceName)};" +
        $"Initial Catalog={datasetName};" +
        $"User ID=app:{settings.ClientId}@{settings.TenantId};" +
        $"Password={settings.ClientSecret};" +
        $"CustomData={customData}";

    private static JsonNode ReadToJson(AdomdDataReader reader)
    {
        var rows = new JsonArray();

        while (reader.Read())
        {
            var row = new JsonObject();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name  = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                row[name] = value switch
                {
                    null           => null,
                    bool b         => JsonValue.Create(b),
                    int n          => JsonValue.Create(n),
                    long n         => JsonValue.Create(n),
                    double d       => JsonValue.Create(d),
                    decimal d      => JsonValue.Create(d),
                    DateTime dt    => JsonValue.Create(dt),
                    _              => JsonValue.Create(value.ToString())
                };
            }
            rows.Add(row);
        }

        return new JsonObject { ["rows"] = rows };
    }
}
