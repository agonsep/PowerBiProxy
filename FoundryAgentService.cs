using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace PowerBiProxy;

public class FoundryAgentService(PowerBiSettings settings)
{
    public async Task<string> AskAsync(string question, JsonNode data, CancellationToken ct = default)
    {
        var credential = new ClientSecretCredential(
            settings.TenantId, settings.ClientId, settings.ClientSecret);

        var agentRef = new AgentReference(
            name: settings.FoundryAgentName,
            version: settings.FoundryAgentVersion);

        var client = new ProjectResponsesClient(
            new Uri(settings.FoundryEndpoint),
            credential,
            agentRef,
            "v1",
            null);

        var input    = BuildMessageBody(question, data);
        var response = await client.CreateResponseAsync(input, cancellationToken: ct);

        return response.Value.GetOutputText();
    }

    private static string BuildMessageBody(string question, JsonNode data) =>
        $"""
        You will answer the question below using only the provided data rows.

        Question:
        {question}

        Data (JSON from ReportDataDev):
        {data.ToJsonString()}
        """;
}
