using CustomerDirectory.Application.Models;
using CustomerDirectory.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerDirectory.Web.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListItemDto>>> Get(
        [FromQuery] string? search, [FromQuery] string sortBy = "fullname",
        [FromQuery] bool descending = false, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _customerService.GetPagedAsync(search, sortBy, descending, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id, CancellationToken ct)
    {
        var customer = await _customerService.GetByIdAsync(id, ct);
        if (customer is null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", id);
            return NotFound(new { message = $"Customer {id} was not found." });
        }
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDetailDto>> Create(CustomerCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var (success, customer, errors) = await _customerService.CreateAsync(dto, ct);
        if (!success)
        {
            foreach (var e in errors!) ModelState.AddModelError(e.Key, string.Join(" ", e.Value));
            return ValidationProblem(ModelState); // 400
        }

        return CreatedAtAction(nameof(GetById), new { id = customer!.Id }, customer);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> Update(int id, CustomerUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var (success, customer, error, notFound) = await _customerService.UpdateAsync(id, dto, ct);
        if (notFound) return NotFound(new { message = $"Customer {id} was not found." });
        if (!success) return Conflict(new { message = error }); // 409

        return Ok(customer);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var (success, notFound) = await _customerService.DeleteAsync(id, ct);
        if (notFound) return NotFound(new { message = $"Customer {id} was not found." });
        return NoContent(); // 204
    }
}