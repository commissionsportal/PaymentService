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
        public async Task<IActionResult> Post([FromQuery] string clientId, [FromBody] Batch batch)
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
                    CallbackTokenExpiration = HttpContext.Request.Headers["x-callbackexpire"]
                };

                await _batchService.ProcessBatch(clientId, batch, headerData);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
