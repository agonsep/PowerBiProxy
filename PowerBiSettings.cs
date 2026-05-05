namespace PowerBiProxy;

public class PowerBiSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = "ab71489d-418c-4bb8-9b64-846c31bd1e35";
    // Required by the effectiveIdentities payload when using RLS with a service principal.
    // Only matters if your RLS DAX uses USERNAME() — for CUSTOMDATA()-only filters any value works.
    public string RlsUsername { get; set; } = string.Empty;
}
