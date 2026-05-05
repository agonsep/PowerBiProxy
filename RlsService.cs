using System.Text.Json.Nodes;
using PowerBiProxy.Models;

namespace PowerBiProxy;

public class RlsService(XmlaService xmla)
{
    private const string ColumnProjection = """
        "AccountID",              ReportDataDev[AccountID],
        "Account Label",            ReportDataDev[Account Label],        
        "Advance Purchase",       ReportDataDev[Advance Purchase],        
        "ArriveCity",            ReportDataDev[ArriveCity],
        "Booking Source",         ReportDataDev[Booking Source],        
        "Bookingdate",            ReportDataDev[Bookingdate],
        "BookingTypeName",        ReportDataDev[BookingTypeName],
        "Cabin",                  ReportDataDev[Cabin],        
        "COL DSAID",              ReportDataDev[COL DSAID],
        "Company Label",          ReportDataDev[Company Label],
        "DataSourceID",           ReportDataDev[DataSourceID],
        "Domestic International", ReportDataDev[Domestic International],
        "Duration",               ReportDataDev[Duration],
        "EndDate",                ReportDataDev[EndDate],
        "InvoiceNumber",          ReportDataDev[InvoiceNumber],
        "IssuedDate",             ReportDataDev[IssuedDate]
        """;



    public Task<JsonNode> GetAllAsync(string datasetName, string dataSourceId) =>
        ExecuteWithRlsAsync(
            $"EVALUATE TOPN(10, SELECTCOLUMNS(ReportDataDev, {ColumnProjection}))",
            datasetName, dataSourceId);

    public Task<JsonNode> GetAccountIdsAsync(string datasetName, string dataSourceId) =>
        ExecuteWithRlsAsync(
            "EVALUATE DISTINCT(SELECTCOLUMNS(ReportDataDev, \"AccountID\", ReportDataDev[AccountID]))",
            datasetName, dataSourceId);

    public Task<JsonNode> FilterByAccountIdsAsync(
        string datasetName, string dataSourceId, AccountIdsFilterRequest request)
    {
        var idList = string.Join(", ", request.AccountIds.Select(id => $"\"{id}\""));
        var dax = $$"""
            EVALUATE
            SELECTCOLUMNS(
                FILTER(ReportDataDev, ReportDataDev[AccountID] IN {{{idList}}}),
                {{ColumnProjection}}
            )
            """;
        return ExecuteWithRlsAsync(dax, datasetName, dataSourceId);
    }

    public Task<JsonNode> FilterByCityAndDateAsync(
        string datasetName, string dataSourceId, CityDateFilterRequest request)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.DepartCity))
            conditions.Add($"ReportDataDev[Depart City] = \"{request.DepartCity}\"");

        if (!string.IsNullOrWhiteSpace(request.ArriveCity))
            conditions.Add($"ReportDataDev[Arrive City] = \"{request.ArriveCity}\"");

        if (request.IssuedDateFrom.HasValue)
            conditions.Add($"ReportDataDev[IssuedDate] >= DATE({request.IssuedDateFrom.Value.Year}, {request.IssuedDateFrom.Value.Month}, {request.IssuedDateFrom.Value.Day})");

        if (request.IssuedDateTo.HasValue)
            conditions.Add($"ReportDataDev[IssuedDate] <= DATE({request.IssuedDateTo.Value.Year}, {request.IssuedDateTo.Value.Month}, {request.IssuedDateTo.Value.Day})");

        var source = conditions.Count > 0
            ? $"FILTER(ReportDataDev, {string.Join(" && ", conditions)})"
            : "ReportDataDev";

        var dax = $"""
            EVALUATE
            SELECTCOLUMNS(
                {source},
                {ColumnProjection}
            )
            """;

        return ExecuteWithRlsAsync(dax, datasetName, dataSourceId);
    }

    private Task<JsonNode> ExecuteWithRlsAsync(string dax, string datasetName, string dataSourceId) =>
        xmla.ExecuteDaxAsync(dax, datasetName, dataSourceId);
}
