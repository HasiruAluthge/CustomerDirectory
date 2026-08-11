using CustomerDirectory.Application.Models;
using CustomerDirectory.Infrastructure.Persistence;
using CustomerDirectory.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class CustomerServiceTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCustomer()
    {
        var db = NewContext();
        var service = new CustomerService(db, NullLogger<CustomerService>.Instance);

        var (success, customer, errors) = await service.CreateAsync(
            new CustomerCreateDto { FullName = "Ada Lovelace", Email = "ada@example.com", Phone = "555-0100" },
            default);

        Assert.True(success);
        Assert.NotNull(customer);
        Assert.StartsWith("CUS-", customer!.CustomerNumber);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsError()
    {
        var db = NewContext();
        var service = new CustomerService(db, NullLogger<CustomerService>.Instance);
        var dto = new CustomerCreateDto { FullName = "Ada Lovelace", Email = "ada@example.com", Phone = "555-0100" };
        await service.CreateAsync(dto, default);

        var (success, _, errors) = await service.CreateAsync(
            new CustomerCreateDto { FullName = "Ada L2", Email = "ADA@example.com", Phone = "555-0101" }, default);

        Assert.False(success);
        Assert.True(errors!.ContainsKey("Email"));
    }

    [Fact]
    public async Task UpdateAsync_WithExistingCustomer_UpdatesFields()
    {
        var db = NewContext();
        var service = new CustomerService(db, NullLogger<CustomerService>.Instance);
        var (_, created, _) = await service.CreateAsync(
            new CustomerCreateDto { FullName = "Ada Lovelace", Email = "ada@example.com", Phone = "555-0100" }, default);

        var (success, updated, error, notFound) = await service.UpdateAsync(created!.Id,
            new CustomerUpdateDto { FullName = "Ada L. Byron", Email = "ada@example.com", Phone = "555-0100", Status = CustomerStatus.Inactive },
            default);

        Assert.True(success);
        Assert.Equal("Ada L. Byron", updated!.FullName);
        Assert.Equal("Inactive", updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingCustomer_ReturnsNotFound()
    {
        var db = NewContext();
        var service = new CustomerService(db, NullLogger<CustomerService>.Instance);

        var (success, _, _, notFound) = await service.UpdateAsync(999,
            new CustomerUpdateDto { FullName = "X", Email = "x@example.com", Phone = "1", Status = CustomerStatus.Active },
            default);

        Assert.False(success);
        Assert.True(notFound);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingCustomer_RemovesIt()
    {
        var db = NewContext();
        var service = new CustomerService(db, NullLogger<CustomerService>.Instance);
        var (_, created, _) = await service.CreateAsync(
            new CustomerCreateDto { FullName = "Ada Lovelace", Email = "ada@example.com", Phone = "555-0100" }, default);

        var (success, notFound) = await service.DeleteAsync(created!.Id, default);

        Assert.True(success);
        Assert.False(notFound);
        Assert.Null(await service.GetByIdAsync(created.Id, default));
    }
}