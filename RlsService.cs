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
        string datasetName, string dataSourceId, AccountIdsFilterRequest request) =>
        ExecuteWithRlsAsync(
            BuildFilteredDax(new AskRequest("", request.AccountIds, null, null, null, null)),
            datasetName, dataSourceId);

    public Task<JsonNode> FilterByCityAndDateAsync(
        string datasetName, string dataSourceId, CityDateFilterRequest request) =>
        ExecuteWithRlsAsync(
            BuildFilteredDax(new AskRequest("",
                null,
                request.DepartCity,
                request.ArriveCity,
                request.IssuedDateFrom,
                request.IssuedDateTo)),
            datasetName, dataSourceId);

    // Used by the /ask endpoint — fetches the rows the LLM will reason over.
    public Task<JsonNode> FetchForAskAsync(
        string datasetName, string dataSourceId, AskRequest request) =>
        ExecuteWithRlsAsync(BuildFilteredDax(request), datasetName, dataSourceId);

    private static string BuildFilteredDax(AskRequest req)
    {
        var conditions = new List<string>();

        if (req.AccountIds is { Count: > 0 })
        {
            var idList = string.Join(", ", req.AccountIds);
            conditions.Add($"ReportDataDev[AccountID] IN {{{idList}}}");
        }
        

        if (!string.IsNullOrWhiteSpace(req.ArriveCity))
            conditions.Add($"ReportDataDev[ArriveCity] = \"{req.ArriveCity}\"");

        if (req.IssuedDateFrom.HasValue)
            conditions.Add($"ReportDataDev[IssuedDate] >= DATE({req.IssuedDateFrom.Value.Year}, {req.IssuedDateFrom.Value.Month}, {req.IssuedDateFrom.Value.Day})");

        if (req.IssuedDateTo.HasValue)
            conditions.Add($"ReportDataDev[IssuedDate] <= DATE({req.IssuedDateTo.Value.Year}, {req.IssuedDateTo.Value.Month}, {req.IssuedDateTo.Value.Day})");

        var source = conditions.Count > 0
            ? $"FILTER(ReportDataDev, {string.Join(" && ", conditions)})"
            : "ReportDataDev";

        return $"""
            EVALUATE
            SELECTCOLUMNS(
                {source},
                {ColumnProjection}
            )
            """;
    }

    private Task<JsonNode> ExecuteWithRlsAsync(string dax, string datasetName, string dataSourceId) =>
        xmla.ExecuteDaxAsync(dax, datasetName, dataSourceId);
}
