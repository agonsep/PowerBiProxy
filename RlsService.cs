using System.Text.Json.Nodes;

namespace PowerBiProxy;

public class RlsService(ApiBasedService apiService, PowerBiSettings settings)
{
    private const string RoleName = "AgencyIdFilter";

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

    public Task<JsonNode> GetAllAsync(string datasetId, string dataSourceId) =>
        ExecuteWithRlsAsync(
            $"EVALUATE TOPN(10, SELECTCOLUMNS(ReportDataDev, {ColumnProjection}))",
            datasetId, dataSourceId);

    public Task<JsonNode> GetAccountIdsAsync(string datasetId, string dataSourceId) =>
        ExecuteWithRlsAsync(
            "EVALUATE DISTINCT(SELECTCOLUMNS(ReportDataDev, \"AccountID\", ReportDataDev[AccountID]))",
            datasetId, dataSourceId);

    private async Task<JsonNode> ExecuteWithRlsAsync(string dax, string datasetId, string dataSourceId)
    {
        // Step 1 — get a service-principal access token
        var spToken = await apiService.GetAccessTokenAsync();

        // Step 2 — exchange it for an embed token that has the RLS effective identity baked in.
        //           customData becomes the value CUSTOMDATA() returns in the DAX RLS filter:
        //               [DataSourceID] == VALUE(CUSTOMDATA())
        var embedToken = await apiService.GenerateEmbedTokenAsync(
            spToken, datasetId,
            username:   settings.RlsUsername,
            roles:      [RoleName],
            customData: dataSourceId);

        // Step 3 — run the DAX query using the embed token (RLS is already baked in)
        return await apiService.ExecuteDaxWithTokenAsync(dax, datasetId, embedToken);
    }
}
