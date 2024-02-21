using Microsoft.AspNetCore.Mvc;
using PaymentService.Interfaces;
using PaymentService.Models;

namespace PaymentService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BatchesController : ControllerBase
    {
        private readonly IBatchService _batchService;

        public BatchesController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] Batch batch)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var headerData = new HeaderData
                {
                    User = HttpContext.Request.Headers["x-user"],
                    Token = HttpContext.Request.Headers["x-token"],
                    CallbackToken = HttpContext.Request.Headers["x-callbacktoken"],
                    CallbackTokenExpiration = HttpContext.Request.Headers["x-callbackexpire"],
                    ClientId = HttpContext.Request.Headers["x-clientid"],
                    CompanyId = HttpContext.Request.Headers["x-companyid"]
                };

                await _batchService.ProcessBatch(batch, headerData);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
