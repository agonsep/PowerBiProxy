using Microsoft.AspNetCore.Mvc;

namespace PowerBiProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class ReportDataQueriesController(PowerBiService pbi) : ControllerBase
{
    private const string DatasetId = "b83d3cf8-4181-470f-b0a6-ad13f81f3068";

    [HttpGet("all")]
    public async Task<IActionResult> All()
    {
        var json = await pbi.GetAllAsync(DatasetId);
        return Content(json, "application/json");
    }

    [HttpGet("filter/{dataSourceId}")]
    public async Task<IActionResult> FilterByDataSourceId(string dataSourceId)
    {
        var json = await pbi.FilterByDataSourceIdAsync(DatasetId, dataSourceId);
        return Content(json, "application/json");
    }

    [HttpGet("datasourceids")]
    public async Task<IActionResult> DataSourceIds()
    {
        var ids = await pbi.GetDistinctDataSourceIdsAsync(DatasetId);
        return Ok(ids);
    }

    [HttpGet("accountids/{dataSourceId}")]
    public async Task<IActionResult> AccountIdsByDataSourceId(string dataSourceId)
    {
        var ids = await pbi.GetAccountIdsByDataSourceIdAsync(DatasetId, dataSourceId);
        return Ok(ids);
    }
}
