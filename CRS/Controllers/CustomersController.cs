using CRS.Models;
using CRS.Services.Customers;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers {
    [ApiController]
    [Route("api/customers")]
    public class CustomersController : ControllerBase {
        private readonly ICustomerService _customers;
        public CustomersController(ICustomerService customers) { _customers = customers; }

        [HttpGet("count")]
        public async Task<IActionResult> Count(CancellationToken ct) {
            var c = await _customers.GetActiveCustomerCountAsync(ct);
            return Ok(new { count = c });
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) {
            // For simplicity, return all active customers (no paging implemented yet)
            // In real app, add paging to service
            var customers = new List<CustomerAccount>(); // Placeholder
            return Ok(new { customers, page, pageSize });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerAccount acct, CancellationToken ct) {
            if (acct == null) return BadRequest("account required");
            var created = await _customers.CreateCustomerAsync(acct, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct) {
            // Placeholder
            return NotFound();
        }
    }
}