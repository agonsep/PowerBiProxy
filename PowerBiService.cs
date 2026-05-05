using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PowerBiProxy;

public class PowerBiService(PowerBiClientFactory clientFactory, PowerBiSettings settings)
{
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

    public Task<string> GetAllAsync(string datasetId) =>
        ExecuteAndSerializeAsync($"""
            EVALUATE
            TOPN(10, SELECTCOLUMNS(ReportDataDev, {ColumnProjection}))
            """, datasetId);

    public Task<string> FilterByDataSourceIdAsync(string datasetId, string dataSourceId) =>
        ExecuteAndSerializeAsync($"""
            EVALUATE
            SELECTCOLUMNS(
                FILTER(ReportDataDev, ReportDataDev[DataSourceID] = {dataSourceId}),
                {ColumnProjection}
            )
            """, datasetId);

    public Task<IList<string>> GetDistinctDataSourceIdsAsync(string datasetId) =>
        ExtractSingleColumnAsync(
            "EVALUATE DISTINCT(SELECTCOLUMNS(ReportDataDev, \"DataSourceID\", ReportDataDev[DataSourceID]))",
            "[DataSourceID]",
            datasetId);

    public Task<IList<string>> GetAccountIdsByDataSourceIdAsync(string datasetId, string dataSourceId) =>
        ExtractSingleColumnAsync($"""
            EVALUATE
            DISTINCT(
                SELECTCOLUMNS(
                    FILTER(ReportDataDev, ReportDataDev[DataSourceID] = {dataSourceId}),
                    "AccountID", ReportDataDev[AccountID]
                )
            )
            """,
            "[AccountID]",
            datasetId);

    private async Task<string> ExecuteAndSerializeAsync(string dax, string datasetId)
    {
        var response = await ExecuteDaxAsync(dax, datasetId);
        return JsonConvert.SerializeObject(response);
    }

    private async Task<IList<string>> ExtractSingleColumnAsync(string dax, string columnKey, string datasetId)
    {
        var response = await ExecuteDaxAsync(dax, datasetId);
        var json = JObject.Parse(JsonConvert.SerializeObject(response));
        var rows = (JArray?)json["results"]?[0]?["tables"]?[0]?["rows"] ?? [];
        return rows.Select(row => row[columnKey]?.ToString() ?? "").ToList();
    }

    private async Task<DatasetExecuteQueriesResponse> ExecuteDaxAsync(string dax, string datasetId)
    {
        var client = await clientFactory.CreateAsync();
        var request = new DatasetExecuteQueriesRequest
        {
            Queries = [new DatasetExecuteQueriesQuery { Query = dax }]
        };
        return await client.Datasets.ExecuteQueriesInGroupAsync(
            Guid.Parse(settings.WorkspaceId),
            datasetId,
            request);
    }
}
