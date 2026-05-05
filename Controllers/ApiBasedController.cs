using Microsoft.AspNetCore.Mvc;

namespace PowerBiProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiBasedController(ApiBasedService pbi) : ControllerBase
{
    private const string DatasetId = "b83d3cf8-4181-470f-b0a6-ad13f81f3068";

    [HttpGet("all")]
    public async Task<IActionResult> All()
    {
        var result = await pbi.GetAllAsync(DatasetId);
        return Ok(result);
    }

    [HttpGet("filter/{dataSourceId}")]
    public async Task<IActionResult> FilterByDataSourceId(string dataSourceId)
    {
        var result = await pbi.FilterByDataSourceIdAsync(DatasetId, dataSourceId);
        return Ok(result);
    }

    [HttpGet("datasourceids")]
    public async Task<IActionResult> DataSourceIds()
    {
        var result = await pbi.GetDistinctDataSourceIdsAsync(DatasetId);
        return Ok(result);
    }

    [HttpGet("accountids/{dataSourceId}")]
    public async Task<IActionResult> AccountIdsByDataSourceId(string dataSourceId)
    {
        var result = await pbi.GetAccountIdsByDataSourceIdAsync(DatasetId, dataSourceId);
        return Ok(result);
    }
}
