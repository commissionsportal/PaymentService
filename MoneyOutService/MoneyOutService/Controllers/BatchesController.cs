using Microsoft.AspNetCore.Mvc;
using MoneyOutService.Inerfaces;
using MoneyOutService.Models;

namespace MoneyOutService.Controllers
{
    [Route("api/v1/{clientId}/[controller]")]
    [ApiController]
    public class BatchesController : ControllerBase
    {
        private readonly IBatchService _batchService;

        public BatchesController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(BatchSummary[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(int clientId, DateTime? begin, DateTime? end, int offset, int count)
        {
            try
            {
                var result = await _batchService.GetBatches(clientId, begin, end, offset, count);
                Response.Headers.Add("Total", result.Count.ToString());
                return Ok(result.Batches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(Batch), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post(int clientId, [FromBody] NewBatch batch)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _batchService.CreateBatch(clientId, batch);
                if (result == null)
                {
                    return NoContent();
                }

                return CreatedAtRoute("GetById", new { id = result.Id, clientId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}", Name = "GetById")]
        [ProducesResponseType(typeof(Batch), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int clientId, long id)
        {
            try
            {
                var batch = await _batchService.GetBatch(clientId, id);
                if (batch == null)
                {
                    return NotFound(new { id });
                }

                return Ok(batch);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
