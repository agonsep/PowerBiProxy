using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;

namespace PowerBiProxy;

public class FoundryAgentService(IHttpClientFactory httpClientFactory, PowerBiSettings settings)
{
    private static readonly string[] Scopes = ["https://ai.azure.com/.default"];
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private const int MaxPollAttempts = 60;

    public async Task<string> AskAsync(string question, JsonNode data, CancellationToken ct = default)
    {
        var token  = await GetTokenAsync(ct);
        var client = httpClientFactory.CreateClient("foundry");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var threadId = await CreateThreadAsync(client, ct);
        await PostUserMessageAsync(client, threadId, BuildMessageBody(question, data), ct);

        var runId = await StartRunAsync(client, threadId, ct);
        await WaitForRunCompletionAsync(client, threadId, runId, ct);

        return await ReadAssistantReplyAsync(client, threadId, ct);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        var credential = new ClientSecretCredential(
            settings.TenantId, settings.ClientId, settings.ClientSecret);
        var token = await credential.GetTokenAsync(new TokenRequestContext(Scopes), ct);
        return token.Token;
    }

    private async Task<string> CreateThreadAsync(HttpClient client, CancellationToken ct)
    {
        var url = $"{settings.FoundryEndpoint}/threads?api-version={settings.FoundryApiVersion}";
        var response = await client.PostAsync(url, JsonContent("{}"), ct);
        var json = await ReadJsonOrThrowAsync(response, "Foundry CreateThread", ct);
        return json["id"]!.GetValue<string>();
    }

    private async Task PostUserMessageAsync(HttpClient client, string threadId, string content, CancellationToken ct)
    {
        var url  = $"{settings.FoundryEndpoint}/threads/{threadId}/messages?api-version={settings.FoundryApiVersion}";
        var body = JsonSerializer.Serialize(new { role = "user", content });
        var response = await client.PostAsync(url, JsonContent(body), ct);
        await ReadJsonOrThrowAsync(response, "Foundry PostMessage", ct);
    }

    private async Task<string> StartRunAsync(HttpClient client, string threadId, CancellationToken ct)
    {
        var url  = $"{settings.FoundryEndpoint}/threads/{threadId}/runs?api-version={settings.FoundryApiVersion}";
        var body = JsonSerializer.Serialize(new { assistant_id = settings.FoundryAgentId });
        var response = await client.PostAsync(url, JsonContent(body), ct);
        var json = await ReadJsonOrThrowAsync(response, "Foundry StartRun", ct);
        return json["id"]!.GetValue<string>();
    }

    private async Task WaitForRunCompletionAsync(HttpClient client, string threadId, string runId, CancellationToken ct)
    {
        var url = $"{settings.FoundryEndpoint}/threads/{threadId}/runs/{runId}?api-version={settings.FoundryApiVersion}";

        for (int attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            var response = await client.GetAsync(url, ct);
            var json = await ReadJsonOrThrowAsync(response, "Foundry GetRun", ct);
            var status = json["status"]?.GetValue<string>() ?? "unknown";

            switch (status)
            {
                case "completed":
                    return;
                case "failed":
                case "cancelled":
                case "expired":
                    var lastError = json["last_error"]?.ToJsonString() ?? "(no error detail)";
                    throw new InvalidOperationException($"Foundry run ended with status '{status}': {lastError}");
            }

            await Task.Delay(PollInterval, ct);
        }

        throw new TimeoutException($"Foundry run {runId} did not complete within {MaxPollAttempts * PollInterval.TotalSeconds:0}s.");
    }

    private async Task<string> ReadAssistantReplyAsync(HttpClient client, string threadId, CancellationToken ct)
    {
        var url = $"{settings.FoundryEndpoint}/threads/{threadId}/messages?api-version={settings.FoundryApiVersion}&order=desc&limit=10";
        var response = await client.GetAsync(url, ct);
        var json = await ReadJsonOrThrowAsync(response, "Foundry ListMessages", ct);

        var data = (JsonArray?)json["data"] ?? [];
        var assistantMsg = data.FirstOrDefault(m => m?["role"]?.GetValue<string>() == "assistant");
        if (assistantMsg is null)
            throw new InvalidOperationException("Foundry returned no assistant message.");

        var contentBlocks = (JsonArray?)assistantMsg["content"] ?? [];
        var sb = new StringBuilder();
        foreach (var block in contentBlocks)
        {
            if (block?["type"]?.GetValue<string>() == "text")
                sb.Append(block["text"]?["value"]?.GetValue<string>());
        }
        return sb.ToString();
    }

    private static string BuildMessageBody(string question, JsonNode data) =>
        $"""
        You will answer the question below using only the provided data rows.

        Question:
        {question}

        Data (JSON from ReportDataDev):
        {data.ToJsonString()}
        """;

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonNode> ReadJsonOrThrowAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{operation} error {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        return JsonNode.Parse(body)!;
    }
}
