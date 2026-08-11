using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomerDirectory.Application.Models;

namespace CustomerDirectory.Application.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken ct);

    Task<CustomerDetailDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<(bool Success, CustomerDetailDto? Customer, IDictionary<string, string[]>? Errors)>
        CreateAsync(CustomerCreateDto dto, CancellationToken ct);

    Task<(bool Success, CustomerDetailDto? Customer, string? Error, bool NotFound)>
        UpdateAsync(int id, CustomerUpdateDto dto, CancellationToken ct);

    Task<(bool Success, bool NotFound)> DeleteAsync(int id, CancellationToken ct);
}