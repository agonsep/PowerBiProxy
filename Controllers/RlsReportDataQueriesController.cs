using Microsoft.AspNetCore.Mvc;

namespace PowerBiProxy.Controllers;

[ApiController]
[Route("[controller]")]
public class RlsReportDataQueriesController(RlsService rls) : ControllerBase
{
    private const string DatasetId = "4e241cdf-6006-4eba-b3d8-e0e5dd54d05a";

    // dataSourceId is passed as customData to the RLS filter:
    //   [DataSourceID] == VALUE(CUSTOMDATA())

    [HttpGet("{dataSourceId}/all")]
    public async Task<IActionResult> All(string dataSourceId)
    {
        var result = await rls.GetAllAsync(DatasetId, dataSourceId);
        return Ok(result);
    }

    [HttpGet("{dataSourceId}/accountids")]
    public async Task<IActionResult> AccountIds(string dataSourceId)
    {
        var result = await rls.GetAccountIdsAsync(DatasetId, dataSourceId);
        return Ok(result);
    }
}
