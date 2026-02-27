using Horizon.Models;
using Horizon.Services.Customers;

using Microsoft.AspNetCore.Mvc;

namespace Horizon.Controllers {
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
        public async Task<IActionResult> List(CancellationToken ct = default) {
            var customers = await _customers.GetAllAsync(ct: ct);
            return Ok(customers);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerAccount acct, CancellationToken ct) {
            if (acct == null) return BadRequest("account required");
            var created = await _customers.CreateAsync(acct, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct) {
            var customer = await _customers.GetByIdAsync(id, ct);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CustomerAccount acct, CancellationToken ct) {
            if (acct == null) return BadRequest("account required");
            acct.Id = id;
            var updated = await _customers.UpdateAsync(acct, ct);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
            var success = await _customers.DeleteAsync(id, ct);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}