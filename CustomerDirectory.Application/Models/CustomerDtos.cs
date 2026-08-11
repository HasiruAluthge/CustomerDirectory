using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    [Required(ErrorMessage = "Full name is required."), StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required."), StringLength(25)]
    [RegularExpression(@"^[0-9+\-\s()]{7,25}$", ErrorMessage = "Phone number can only contain digits, spaces, and + - ( ) symbols.")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Address { get; set; }
}
public class CustomerUpdateDto : CustomerCreateDto
{
    public CustomerStatus Status { get; set; }
}
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);