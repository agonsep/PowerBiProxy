namespace PowerBiProxy;

public class PowerBiSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = "ab71489d-418c-4bb8-9b64-846c31bd1e35";
    // Display name of the workspace — used for the XMLA connection string
    public string WorkspaceName { get; set; } = string.Empty;
}
