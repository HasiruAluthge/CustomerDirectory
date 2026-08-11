using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerDirectory.Application.Models;

public record CustomerListItemDto(
    int Id,
    string CustomerNumber,
    string FullName,
    string Email,
    string Phone,
    string Status,
    DateTime UpdatedAtUtc);

public record CustomerDetailDto(
    int Id,
    string CustomerNumber,
    string FullName,
    string Email,
    string Phone,
    string? Address,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public class CustomerCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class CustomerUpdateDto : CustomerCreateDto
{
    public CustomerStatus Status { get; set; }
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);