using Microsoft.AspNetCore.Mvc;
using PowerBiProxy.Models;

namespace PowerBiProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class RlsReportDataQueriesController(RlsService rls) : ControllerBase
{    
    private const string DatasetName      = "Spend Summary Air";
    private const string DataSourceHeader = "X-DataSource-Id";

    [HttpGet("all")]
    public async Task<IActionResult> All()
    {
        if (!TryGetDataSourceId(out var dataSourceId))
            return BadRequest($"Missing required header: {DataSourceHeader}");

        var result = await rls.GetAllAsync(DatasetName, dataSourceId);
        return Ok(result);
    }

    [HttpGet("accountids")]
    public async Task<IActionResult> AccountIds()
    {
        if (!TryGetDataSourceId(out var dataSourceId))
            return BadRequest($"Missing required header: {DataSourceHeader}");

        var result = await rls.GetAccountIdsAsync(DatasetName, dataSourceId);
        return Ok(result);
    }

    [HttpPost("filter/accountids")]
    public async Task<IActionResult> FilterByAccountIds([FromBody] AccountIdsFilterRequest request)
    {
        if (!TryGetDataSourceId(out var dataSourceId))
            return BadRequest($"Missing required header: {DataSourceHeader}");

        if (request.AccountIds is not { Count: > 0 })
            return BadRequest("At least one AccountId is required.");

        var result = await rls.FilterByAccountIdsAsync(DatasetName, dataSourceId, request);
        return Ok(result);
    }

    [HttpPost("filter/citydate")]
    public async Task<IActionResult> FilterByCityAndDate([FromBody] CityDateFilterRequest request)
    {
        if (!TryGetDataSourceId(out var dataSourceId))
            return BadRequest($"Missing required header: {DataSourceHeader}");

        var result = await rls.FilterByCityAndDateAsync(DatasetName, dataSourceId, request);
        return Ok(result);
    }

    private bool TryGetDataSourceId(out string dataSourceId)
    {
        dataSourceId = Request.Headers[DataSourceHeader].FirstOrDefault() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(dataSourceId);
    }
}
