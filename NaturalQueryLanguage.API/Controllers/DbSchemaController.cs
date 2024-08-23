using Microsoft.AspNetCore.Mvc;
using NaturalQueryLanguage.Business;

namespace NaturalQueryLanguage.Controllers;

[Route("[controller]")]
[ApiController]
public class DbSchemaController(DbSchemaService service)
    : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> CreateOrUpdate(
        [FromHeader(Name = "X-PROVIDER")] string provider,
        [FromHeader(Name = "X-CONNECTION-STRING")] string connectionString)
    {
        await service.CreateOrUpdate(provider, connectionString);
        return Ok();
    }
}