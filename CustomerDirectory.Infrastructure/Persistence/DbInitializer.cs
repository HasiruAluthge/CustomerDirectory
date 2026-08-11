using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomerDirectory.Application.Models;

namespace CustomerDirectory.Infrastructure.Persistence;

public static class DbInitializer
{
    public static void Seed(AppDbContext context)
    {
        if (context.Customers.Any()) return; // don't reseed

        var rnd = new Random(42); // fixed seed => reproducible dev data
        var statuses = new[] { CustomerStatus.Active, CustomerStatus.Active, CustomerStatus.Inactive };
        var customers = Enumerable.Range(1, 3).Select(i => new Customer
        {
            CustomerNumber = $"CUS-{i:D5}",
            FullName = $"Seed Customer {i}",
            Email = $"seed.customer{i}@example.com",
            Phone = $"+1-555-01{i:D2}",
            Address = i % 5 == 0 ? null : $"{i} Example Street",
            Status = statuses[rnd.Next(statuses.Length)],
            CreatedAtUtc = DateTime.UtcNow.AddDays(-rnd.Next(1, 200)),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-rnd.Next(0, 30))
        });

        context.Customers.AddRange(customers);
        context.SaveChanges();
    }
}
