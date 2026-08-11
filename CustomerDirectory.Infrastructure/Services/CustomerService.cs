using CustomerDirectory.Application.Models;
using CustomerDirectory.Application.Services;
using CustomerDirectory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerDirectory.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(AppDbContext db, ILogger<CustomerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.CustomerNumber.ToLower().Contains(term));
        }

        query = sortBy.ToLower() switch
        {
            "customernumber" => descending ? query.OrderByDescending(c => c.CustomerNumber) : query.OrderBy(c => c.CustomerNumber),
            "email" => descending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "status" => descending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "updatedatutc" => descending ? query.OrderByDescending(c => c.UpdatedAtUtc) : query.OrderBy(c => c.UpdatedAtUtc),
            _ => descending ? query.OrderByDescending(c => c.FullName) : query.OrderBy(c => c.FullName),
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItemDto(
                c.Id, c.CustomerNumber, c.FullName, c.Email, c.Phone, c.Status.ToString(), c.UpdatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : ToDetailDto(c);
    }

    public async Task<(bool, CustomerDetailDto?, IDictionary<string, string[]>?)> CreateAsync(
        CustomerCreateDto dto, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (await EmailExistsAsync(dto.Email, null, ct))
            errors["Email"] = new[] { "A customer with this email already exists." };

        if (errors.Count > 0) return (false, null, errors);

        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            CustomerNumber = await GenerateCustomerNumberAsync(ct),
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            Address = dto.Address?.Trim(),
            Status = CustomerStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Customer {CustomerId} ({CustomerNumber}) created", customer.Id, customer.CustomerNumber);
        return (true, ToDetailDto(customer), null);
    }

    public async Task<(bool, CustomerDetailDto?, string?, bool)> UpdateAsync(
        int id, CustomerUpdateDto dto, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            _logger.LogWarning("Update failed: customer {CustomerId} not found", id);
            return (false, null, null, true);
        }

        if (await EmailExistsAsync(dto.Email, id, ct))
        {
            _logger.LogWarning("Update rejected: duplicate email for customer {CustomerId}", id);
            return (false, null, "A customer with this email already exists.", false);
        }

        customer.FullName = dto.FullName.Trim();
        customer.Email = dto.Email.Trim();
        customer.Phone = dto.Phone.Trim();
        customer.Address = dto.Address?.Trim();
        customer.Status = dto.Status;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Customer {CustomerId} updated", id);
        return (true, ToDetailDto(customer), null, false);
    }

    public async Task<(bool, bool)> DeleteAsync(int id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return (false, true);

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Customer {CustomerId} deleted", id);
        return (true, false);
    }

    private async Task<bool> EmailExistsAsync(string email, int? excludingId, CancellationToken ct)
    {
        var normalized = email.Trim().ToLower();
        return await _db.Customers.AsNoTracking().AnyAsync(c =>
            c.Email.ToLower() == normalized && (excludingId == null || c.Id != excludingId), ct);
    }

    private async Task<string> GenerateCustomerNumberAsync(CancellationToken ct)
    {
        var count = await _db.Customers.CountAsync(ct);
        return $"CUS-{(count + 1):D5}";
    }

    private static CustomerDetailDto ToDetailDto(Customer c) => new(
        c.Id, c.CustomerNumber, c.FullName, c.Email, c.Phone, c.Address,
        c.Status.ToString(), c.CreatedAtUtc, c.UpdatedAtUtc);
}